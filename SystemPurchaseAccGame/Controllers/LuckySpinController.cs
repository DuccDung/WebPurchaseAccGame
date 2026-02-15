using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SystemPurchaseAccGame.Models;

[Authorize]
public class LuckySpinController : Controller
{
    private readonly GameAccShopContext _context;

    public LuckySpinController(GameAccShopContext context)
    {
        _context = context;
    }

    // View vòng quay
    [HttpGet]
    public IActionResult Index() => View();

    // List items để client vẽ wheel (KHÔNG trả acc/pass)
    [HttpGet]
    [Route("api/luckyspin/items")]
    public async Task<IActionResult> GetItems()
    {
        var items = await _context.LuckySpinItems
            .AsNoTracking()
            .Where(x => x.IsActive && x.Weight > 0 && (x.Remaining == null || x.Remaining > 0))
            .OrderBy(x => x.ItemId)
            .Select(x => new
            {
                id = x.ItemId,
                label = x.Title,
                tier = x.PrizeTier,
                value = x.PrizeValue
            })
            .ToListAsync();

        return Ok(new { ok = true, items });
    }

    public class SpinResultDto
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public int Index { get; set; }           // index trúng trong danh sách items
        public object? Prize { get; set; }       // chứa acc/pass để show cho user
    }

    // Quay
    [HttpPost]
    [Route("api/luckyspin/spin")]
    public async Task<IActionResult> Spin()
    {
        // Auth
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
            return Unauthorized(new { ok = false, message = "Bạn chưa đăng nhập." });

        // Lấy danh sách item hợp lệ (cùng thứ tự với GetItems để trả Index)
        var items = await _context.LuckySpinItems
            .Where(x => x.IsActive && x.Weight > 0 && (x.Remaining == null || x.Remaining > 0))
            .OrderBy(x => x.ItemId)
            .ToListAsync();

        if (items.Count == 0)
            return Ok(new SpinResultDto { Ok = false, Message = "Hiện không có phần thưởng khả dụng." });

        // Chọn theo trọng số
        int total = items.Sum(x => Math.Max(0, x.Weight));
        if (total <= 0)
            return Ok(new SpinResultDto { Ok = false, Message = "Tất cả phần thưởng đang để Weight=0." });

        int r = Random.Shared.Next(1, total + 1);
        int acc = 0;
        int winIndex = -1;
        LuckySpinItem? win = null;

        for (int i = 0; i < items.Count; i++)
        {
            acc += Math.Max(0, items[i].Weight);
            if (r <= acc)
            {
                win = items[i];
                winIndex = i;
                break;
            }
        }

        if (win == null || winIndex < 0)
            return Ok(new SpinResultDto { Ok = false, Message = "Không chọn được phần thưởng, thử lại." });

        // Transaction: trừ Remaining an toàn (tránh 2 người trúng cùng lúc)
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // lock item (load lại trong transaction)
            var locked = await _context.LuckySpinItems
                .FirstOrDefaultAsync(x => x.ItemId == win.ItemId);

            if (locked == null || !locked.IsActive || locked.Weight <= 0)
            {
                await tx.RollbackAsync();
                return Ok(new SpinResultDto { Ok = false, Message = "Phần thưởng vừa bị thay đổi, thử lại." });
            }

            if (locked.Remaining != null && locked.Remaining <= 0)
            {
                await tx.RollbackAsync();
                return Ok(new SpinResultDto { Ok = false, Message = "Phần thưởng vừa hết, thử lại." });
            }

            // ✅ Trúng -> trừ Remaining (nếu admin set Remaining=1 thì trúng xong sẽ tự ẩn)
            if (locked.Remaining != null)
                locked.Remaining -= 1;

            // lưu dấu vết (trong 1 bảng)
            locked.LastWinnerUserId = userId;
            locked.LastWonAt = DateTime.UtcNow;
            locked.UpdatedAt = DateTime.UtcNow;

            // (Tuỳ chọn) Nếu bạn muốn “trúng là ẩn luôn” không cần Remaining:
            // locked.IsActive = false;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new SpinResultDto
            {
                Ok = true,
                Message = locked.WinMessage ?? "Chúc mừng bạn đã trúng thưởng!",
                Index = winIndex,
                Prize = new
                {
                    id = locked.ItemId,
                    title = locked.Title,
                    tier = locked.PrizeTier,
                    value = locked.PrizeValue,
                    user = locked.AccountUser,
                    pass = locked.AccountPass
                }
            });
        }
        catch
        {
            await tx.RollbackAsync();
            return StatusCode(500, new SpinResultDto { Ok = false, Message = "Lỗi hệ thống khi quay." });
        }
    }
}
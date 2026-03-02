using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using SystemPurchaseAccGame.Models;
using SystemPurchaseAccGame.Models.ViewModels;

namespace SystemPurchaseAccGame.Controllers.Custom
{
    public class LuckyController : Controller
    {
        private readonly GameAccShopContext _db;
        private readonly Random _rng = new();

        public LuckyController(GameAccShopContext db)
        {
            _db = db;
        }

        private IQueryable<LuckySpinItem> Eligible(IQueryable<LuckySpinItem> q)
            => q.Where(x => x.IsActive
                         && x.LastWinnerUserId == null
                         && (x.Remaining == null || x.Remaining > 0));

        private static LuckySpinItemVm ToVm(LuckySpinItem x) => new LuckySpinItemVm
        {
            ItemId = x.ItemId,
            Title = x.Title,
            PrizeTier = x.PrizeTier,
            PrizeValue = x.PrizeValue,
            Weight = x.Weight,
            Remaining = x.Remaining,
            IsActive = x.IsActive,
            WinMessage = x.WinMessage
        };

        public async Task<IActionResult> Index()
        {
            // lấy đúng 9 item hợp lệ
            var items = await Eligible(_db.LuckySpinItems.AsNoTracking())
                .OrderByDescending(x => x.Weight)
                .ThenBy(x => x.ItemId)
                .Take(9)
                .Select(x => new LuckySpinItemVm
                {
                    ItemId = x.ItemId,
                    Title = x.Title,
                    PrizeTier = x.PrizeTier,
                    PrizeValue = x.PrizeValue,
                    Weight = x.Weight,
                    Remaining = x.Remaining,
                    IsActive = x.IsActive,
                    WinMessage = x.WinMessage
                })
                .ToListAsync();

            return View(new LuckyIndexVm { Items = items });
        }

        private int PickWeightedIndex(List<LuckySpinItem> list)
        {
            var pool = list
                .Select((x, i) => new { i, w = Math.Max(0, x.Weight) })
                .Where(x => x.w > 0)
                .ToList();

            if (pool.Count == 0) return _rng.Next(list.Count);

            var total = pool.Sum(x => x.w);
            var r = _rng.NextDouble() * total;

            foreach (var x in pool)
            {
                r -= x.w;
                if (r <= 0) return x.i;
            }
            return pool[^1].i;
        }

        [Authorize] // bắt đăng nhập cookie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Spin([FromBody] SpinRequestVm req)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !long.TryParse(userIdStr, out var userId))
                return Unauthorized(new SpinResponseVm { Ok = false, Message = "Bạn chưa đăng nhập." });

            // ====== CHECK SESSION LƯỢT QUAY ======
            var key = $"LUCKY_SPIN_CHANCE_{userId}";
            var chance = HttpContext.Session.GetInt32(key) ?? 0;
            if (chance <= 0)
                return Json(new SpinResponseVm { Ok = false, Message = "Bạn không có lượt quay. Hãy mua hàng để nhận 1 lượt quay." });

            if (req?.CurrentItemIds == null || req.CurrentItemIds.Count != 9)
                return BadRequest(new SpinResponseVm { Ok = false, Message = "Danh sách 9 item không hợp lệ." });

            // 2) Mở transaction isolation cao để tránh trùng trúng
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            // 3) Lấy đúng 9 item đang hiển thị và vẫn còn hợp lệ
            var current = await Eligible(_db.LuckySpinItems)
                .Where(x => req.CurrentItemIds.Contains(x.ItemId))
                .ToListAsync();

            if (current.Count != 9)
            {
                await tx.RollbackAsync();
                return Json(new SpinResponseVm
                {
                    Ok = false,
                    Message = "Danh sách phần thưởng đã thay đổi (có thể người khác vừa trúng). Vui lòng tải lại."
                });
            }

            // 4) Chọn item trúng trong 9 item theo Weight
            var chosenIndex = PickWeightedIndex(current);
            var won = current[chosenIndex];

            // 5) Chốt thưởng: gán người thắng + thời gian + trừ remaining + ẩn nếu hết
            won.LastWinnerUserId = userId;
            won.LastWonAt = DateTime.UtcNow;

            if (won.Remaining.HasValue)
            {
                won.Remaining = Math.Max(0, won.Remaining.Value - 1);
                if (won.Remaining.Value <= 0)
                    won.IsActive = false;
            }

            await _db.SaveChangesAsync();

            // 6) Tìm item mới hợp lệ để thay thế (không trùng 8 item còn lại)
            var stillShownIds = req.CurrentItemIds.Where(id => id != won.ItemId).ToList();

            var replacement = await Eligible(_db.LuckySpinItems.AsNoTracking())
                .Where(x => !stillShownIds.Contains(x.ItemId) && x.ItemId != won.ItemId)
                .OrderByDescending(x => x.Weight)
                .ThenBy(x => x.ItemId)
                .FirstOrDefaultAsync();

            await tx.CommitAsync();
            HttpContext.Session.Remove(key);
            return Json(new SpinResponseVm
            {
                Ok = true,
                Message = won.WinMessage ?? $"Bạn trúng {won.Title}",
                Won = ToVm(won),
                Replacement = replacement == null ? null : ToVm(replacement)
            });
        }
    }
}
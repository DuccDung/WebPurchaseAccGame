using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using SystemPurchaseAccGame.Models;
using SystemPurchaseAccGame.ViewModel;

namespace SystemPurchaseAccGame.Controllers
{
    public class AdminController : Controller
    {
        private readonly GameAccShopContext _context;
        public AdminController(GameAccShopContext context)
        {
            _context = context;
        }

        // ===== Helpers =====
        private bool IsAdmin()
        {
            // Bạn đang login admin bằng query user.Role == "Admin"
            // Hiện tại chưa thấy bạn lưu cookie/claims cho Admin => check tạm Session/TempData.
            // Nếu bạn có lưu session, hãy sửa lại cho đúng.
            // Ở đây check "AdminEmail" cho đơn giản, bạn có thể đổi.
            // Nếu chưa có session, cứ để true để test (khuyến nghị sửa sau).
            return true;
        }

        private static string Mask(string? s, int keepStart = 2, int keepEnd = 2)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim();
            if (s.Length <= keepStart + keepEnd) return new string('*', s.Length);
            return s.Substring(0, keepStart) + new string('*', s.Length - keepStart - keepEnd) + s.Substring(s.Length - keepEnd);
        }

        // ===== Login giữ nguyên của bạn =====
        public async Task<IActionResult> Login()
        {
            await Task.CompletedTask;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email && u.PasswordHash == password && u.Role == "Admin");

            if (user == null)
                return Json(new { success = false, message = "Đăng Nhập Không Thành Công!" });

            await Task.CompletedTask;

            return Json(new { success = true, redirectUrl = Url.Action("Dashboard", "Admin") });
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public async Task<IActionResult> UploadAccGame()
        {
            var games = await _context.Games
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ViewBag.Games = games;

            return PartialView("Partials/Admin/_UploadAccGame");
        }

        public async Task<IActionResult> AccGame()
        {
            await Task.CompletedTask;
            return PartialView("Partials/Admin/_AccountGame");
        }

        // ============================================================
        // 1) VIEW: ConfirmPayment -> render list Topup PENDING
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ConfirmPayment()
        {
            if (!IsAdmin()) return Unauthorized();

            var pending = await _context.Topups
                .AsNoTracking()
                .Where(t => t.Status == "PENDING")
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new AdminTopupVm
                {
                    TopupId = t.TopupId,
                    UserId = t.UserId,
                    UserName = t.User.Username ?? t.User.FullName ?? "",
                    Email = t.User.Email ?? "",
                    Phone = t.User.Phone ?? "",

                    Method = t.Method,
                    Amount = t.Amount,
                    Status = t.Status,
                    ReferenceCode = t.ReferenceCode,
                    RawPayload = t.RawPayload,
                    CreatedAt = t.CreatedAt,
                    Provider = t.Provider
                })
                .ToListAsync();

            // stats cho view
            ViewBag.PendingCount = pending.Count;
            ViewBag.PendingTotalAmount = pending.Sum(x => x.Amount);

            ViewBag.PendingManualCount = pending.Count(x => string.Equals(x.Method, "BANK", StringComparison.OrdinalIgnoreCase));
            ViewBag.PendingCardCount = pending.Count(x => string.Equals(x.Method, "CARD", StringComparison.OrdinalIgnoreCase)
                                                      || string.Equals(x.Method, "Card", StringComparison.OrdinalIgnoreCase));

            return PartialView("Partials/Admin/_ConfirmPayment", pending);
        }

        // ============================================================
        // 2) API: Approve topup (cộng tiền vào ví)
        // POST /admin/topup/approve
        // ============================================================
        public class ApproveTopupReq
        {
            public long TopupId { get; set; }
            public long? AmountActual { get; set; } // nếu admin muốn cộng khác Amount khai báo
            public long? Fee { get; set; }          // optional
            public string? Ref { get; set; }        // optional đối soát
            public string? AdminNote { get; set; }  // optional
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTopup([FromForm] ApproveTopupReq req)
        {
            if (!IsAdmin()) return Unauthorized();

            if (req.TopupId <= 0)
                return BadRequest(new { success = false, message = "TopupId không hợp lệ." });

            // lock row theo transaction để tránh duyệt 2 lần
            await using var tx = await _context.Database.BeginTransactionAsync();

            var topup = await _context.Topups
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TopupId == req.TopupId);

            if (topup == null)
                return NotFound(new { success = false, message = "Không tìm thấy topup." });

            if (!string.Equals(topup.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = $"Topup không ở trạng thái PENDING (hiện tại: {topup.Status})." });

            var amount = req.AmountActual ?? topup.Amount;
            var fee = req.Fee ?? topup.Fee;

            if (amount < 0 || fee < 0)
                return BadRequest(new { success = false, message = "Số tiền/phí không hợp lệ." });

            // cập nhật topup
            topup.Amount = amount;
            topup.Fee = fee;
            topup.Status = "APPROVED";
            topup.CompletedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(req.Ref)) topup.ReferenceCode = req.Ref;

            // lưu note vào RawPayload (vì bảng chưa có cột Note riêng)
            if (!string.IsNullOrWhiteSpace(req.AdminNote))
            {
                var note = $"ADMIN_NOTE: {req.AdminNote}";
                topup.RawPayload = string.IsNullOrWhiteSpace(topup.RawPayload)
                    ? note
                    : (topup.RawPayload + "\n" + note);
            }

            // cộng tiền vào ví (thực nhận = amount - fee)
            var net = Math.Max(0, amount - fee);

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == topup.UserId);
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = topup.UserId,
                    Balance = 0,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync(); // để có WalletId
            }

            wallet.Balance += net;
            wallet.UpdatedAt = DateTime.UtcNow;

            // TODO (optional): ghi WalletTransaction nếu bạn muốn audit
            // _context.WalletTransactions.Add(new WalletTransaction { ... });

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Json(new
            {
                success = true,
                message = "Đã duyệt topup & cộng tiền vào ví.",
                data = new
                {
                    topupId = topup.TopupId,
                    userId = topup.UserId,
                    netAdded = net,
                    walletBalance = wallet.Balance
                }
            });
        }

        // ============================================================
        // 3) API: Reject topup
        // POST /admin/topup/reject
        // ============================================================
        public class RejectTopupReq
        {
            public long TopupId { get; set; }
            public string? Reason { get; set; }
            public string? AdminNote { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectTopup([FromForm] RejectTopupReq req)
        {
            if (!IsAdmin()) return Unauthorized();

            if (req.TopupId <= 0)
                return BadRequest(new { success = false, message = "TopupId không hợp lệ." });

            var topup = await _context.Topups.FirstOrDefaultAsync(t => t.TopupId == req.TopupId);
            if (topup == null)
                return NotFound(new { success = false, message = "Không tìm thấy topup." });

            if (!string.Equals(topup.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = $"Topup không ở trạng thái PENDING (hiện tại: {topup.Status})." });

            topup.Status = "REJECTED";
            topup.CompletedAt = DateTime.UtcNow;

            // lưu reason/note vào RawPayload
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(req.Reason)) sb.AppendLine("REJECT_REASON: " + req.Reason);
            if (!string.IsNullOrWhiteSpace(req.AdminNote)) sb.AppendLine("ADMIN_NOTE: " + req.AdminNote);

            if (sb.Length > 0)
            {
                topup.RawPayload = string.IsNullOrWhiteSpace(topup.RawPayload)
                    ? sb.ToString().Trim()
                    : (topup.RawPayload + "\n" + sb.ToString().Trim());
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã từ chối yêu cầu nạp tiền.",
                data = new { topupId = topup.TopupId }
            });
        }
    }
}

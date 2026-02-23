using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SystemPurchaseAccGame.Models;
using SystemPurchaseAccGame.ViewModel;

namespace SystemPurchaseAccGame.Controllers
{
    public class AdminController : Controller
    {
        private readonly GameAccShopContext _context;

        // ====== Session Keys ======
        private const string S_ADMIN_ID = "ADMIN_ID";
        private const string S_ADMIN_EMAIL = "ADMIN_EMAIL";
        private const string S_ADMIN_ROLE = "ADMIN_ROLE";

        public AdminController(GameAccShopContext context)
        {
            _context = context;
        }

        // =========================
        // Helpers: Auth / Session
        // =========================
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString(S_ADMIN_ROLE);
            var id = HttpContext.Session.GetInt32(S_ADMIN_ID);
            return id.HasValue && id.Value > 0 && string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult RequireAdminView()
        {
            if (IsAdmin()) return null!;
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RequireAdminJson()
        {
            if (IsAdmin()) return null!;
            return Unauthorized(new { success = false, message = "Vui lòng đăng nhập Admin." });
        }

        // =========================
        // Login / Logout
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã login rồi thì đá thẳng qua dashboard
            if (IsAdmin()) return RedirectToAction(nameof(Dashboard));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            email = (email ?? "").Trim();

            // NOTE: bạn đang so passwordHash == password (plain). Mình giữ nguyên theo code của bạn.
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Email == email &&
                    u.PasswordHash == password &&
                    u.Role == "Admin"
                );

            if (user == null)
                return Json(new { success = false, message = "Đăng Nhập Không Thành Công!" });

            // ===== Lưu session =====
            HttpContext.Session.SetInt32(S_ADMIN_ID, (int)user.UserId);
            HttpContext.Session.SetString(S_ADMIN_EMAIL, user.Email ?? "");
            HttpContext.Session.SetString(S_ADMIN_ROLE, user.Role ?? "Admin");

            // rememberMe: bạn muốn thì sau này đổi sang cookie auth; hiện tại dùng session nên ignore
            await Task.CompletedTask;

            return Json(new { success = true, redirectUrl = Url.Action(nameof(Dashboard), "Admin") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // =========================
        // Dashboard
        // =========================
        public async Task<IActionResult> Dashboard()
        {
            var guard = RequireAdminView();
            if (guard != null) return guard;

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var paidThisMonth = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == "PAID" && o.CreatedAt >= monthStart)
                .ToListAsync();

            ViewBag.RevenueMonth = paidThisMonth.Sum(x => x.TotalAmount);
            ViewBag.SoldCountMonth = paidThisMonth.Count;

            var pendingTopups = await _context.Topups
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

            ViewBag.PendingTopups = pendingTopups;

            var recentPaidOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == "PAID")
                .Include(o => o.User)
                .OrderByDescending(o => o.PaidAt ?? o.CreatedAt)
                .Select(o => new
                {
                    o.OrderId,
                    UserName = o.User.Username ?? o.User.FullName ?? "Khách",
                    o.TotalAmount,
                    o.CreatedAt,
                    o.PaidAt
                })
                .Take(5)
                .ToListAsync();

            ViewBag.RecentPaidOrders = recentPaidOrders;

            return View();
        }

        // =========================
        // Partials
        // =========================
        public async Task<IActionResult> UploadAccGame()
        {
            var guard = RequireAdminView();
            if (guard != null) return guard;

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
            var guard = RequireAdminView();
            if (guard != null) return guard;

            await Task.CompletedTask;
            return PartialView("Partials/Admin/_AccountGame");
        }

        // ============================================================
        // VIEW: ConfirmPayment
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ConfirmPayment()
        {
            var guard = RequireAdminView();
            if (guard != null) return guard;

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

            ViewBag.PendingCount = pending.Count;
            ViewBag.PendingTotalAmount = pending.Sum(x => x.Amount);

            ViewBag.PendingManualCount = pending.Count(x => string.Equals(x.Method, "BANK", StringComparison.OrdinalIgnoreCase));
            ViewBag.PendingCardCount = pending.Count(x => string.Equals(x.Method, "CARD", StringComparison.OrdinalIgnoreCase)
                                                      || string.Equals(x.Method, "Card", StringComparison.OrdinalIgnoreCase));

            return PartialView("Partials/Admin/_ConfirmPayment", pending);
        }

        // ============================================================
        // API: Approve / Reject
        // ============================================================
        public class ApproveTopupJson
        {
            public long TopupId { get; set; }
            public long? AmountActual { get; set; }
            public long? Fee { get; set; }
            public string? Ref { get; set; }
            public string? AdminNote { get; set; }
        }

        [HttpPost]
        [Route("/admin/api/topup/approve")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiApproveTopup([FromBody] ApproveTopupJson req)
        {
            var guard = RequireAdminJson();
            if (guard != null) return guard;

            if (req == null || req.TopupId <= 0)
                return BadRequest(new { success = false, message = "TopupId không hợp lệ." });

            await using var tx = await _context.Database.BeginTransactionAsync();

            var topup = await _context.Topups.FirstOrDefaultAsync(t => t.TopupId == req.TopupId);
            if (topup == null)
                return NotFound(new { success = false, message = "Không tìm thấy yêu cầu topup." });

            if (!string.Equals(topup.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = $"Topup không ở trạng thái PENDING (hiện tại: {topup.Status})." });

            var amount = req.AmountActual ?? topup.Amount;
            var fee = req.Fee ?? topup.Fee;

            if (amount < 0 || fee < 0)
                return BadRequest(new { success = false, message = "Số tiền/phí không hợp lệ." });

            topup.Amount = amount;
            topup.Status = "SUCCESS";
            topup.CompletedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(req.Ref))
                topup.ReferenceCode = req.Ref.Trim();

            if (!string.IsNullOrWhiteSpace(req.AdminNote))
            {
                var note = $"ADMIN_NOTE: {req.AdminNote.Trim()}";
                topup.RawPayload = string.IsNullOrWhiteSpace(topup.RawPayload)
                    ? note
                    : (topup.RawPayload + "\n" + note);
            }

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
                await _context.SaveChangesAsync();
            }

            wallet.Balance += net;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Json(new
            {
                success = true,
                message = "Đã xác thực & cộng tiền vào ví.",
                data = new
                {
                    topupId = topup.TopupId,
                    userId = topup.UserId,
                    amount = amount,
                    fee = fee,
                    netAdded = net,
                    walletBalance = wallet.Balance
                }
            });
        }

        public class RejectTopupJson
        {
            public long TopupId { get; set; }
            public string? Reason { get; set; }
            public string? AdminNote { get; set; }
        }

        [HttpPost]
        [Route("/admin/api/topup/reject")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiRejectTopup([FromBody] RejectTopupJson req)
        {
            var guard = RequireAdminJson();
            if (guard != null) return guard;

            if (req == null || req.TopupId <= 0)
                return BadRequest(new { success = false, message = "TopupId không hợp lệ." });

            var topup = await _context.Topups.FirstOrDefaultAsync(t => t.TopupId == req.TopupId);
            if (topup == null)
                return NotFound(new { success = false, message = "Không tìm thấy yêu cầu topup." });

            if (!string.Equals(topup.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = $"Topup không ở trạng thái PENDING (hiện tại: {topup.Status})." });

            topup.Status = "FAILED";
            topup.CompletedAt = DateTime.UtcNow;

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(req.Reason)) sb.AppendLine("REJECT_REASON: " + req.Reason.Trim());
            if (!string.IsNullOrWhiteSpace(req.AdminNote)) sb.AppendLine("ADMIN_NOTE: " + req.AdminNote.Trim());

            if (sb.Length > 0)
            {
                var extra = sb.ToString().Trim();
                topup.RawPayload = string.IsNullOrWhiteSpace(topup.RawPayload)
                    ? extra
                    : (topup.RawPayload + "\n" + extra);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã từ chối yêu cầu nạp tiền.",
                data = new { topupId = topup.TopupId }
            });
        }

        // =========================
        // VIEW: Category (Partial)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Category()
        {
            var guard = RequireAdminView();
            if (guard != null) return guard;

            var list = await _context.GameCategories
                .AsNoTracking()
                .OrderByDescending(x => x.CategoryId)
                .Select(x => new CategoryRowVm
                {
                    CategoryId = x.CategoryId,
                    Name = x.Name,
                    Slug = x.Slug,
                    CreatedAt = x.CreatedAt,
                    GameCount = x.Games.Count
                })
                .ToListAsync();

            return PartialView("Partials/Admin/_Category", list);
        }

        [HttpGet]
        [Route("/admin/api/category/list")]
        public async Task<IActionResult> ApiCategoryList()
        {
            var guard = RequireAdminJson();
            if (guard != null) return guard;

            var list = await _context.GameCategories
                .AsNoTracking()
                .OrderByDescending(x => x.CategoryId)
                .Select(x => new
                {
                    x.CategoryId,
                    x.Name,
                    x.Slug,
                    CreatedAt = x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    GameCount = x.Games.Count
                })
                .ToListAsync();

            return Json(new { success = true, data = list });
        }

        public class CategoryCreateJson { public string? Name { get; set; } public string? Slug { get; set; } }

        [HttpPost]
        [Route("/admin/api/category/create")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiCategoryCreate([FromBody] CategoryCreateJson req)
        {
            var guard = RequireAdminJson();
            if (guard != null) return guard;

            var name = (req?.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 80)
                return BadRequest(new { success = false, message = "Tên danh mục không hợp lệ (1-80 ký tự)." });

            var slug = (req?.Slug ?? "").Trim();
            if (string.IsNullOrWhiteSpace(slug))
                slug = Slugify(name);
            if (slug.Length > 120)
                return BadRequest(new { success = false, message = "Slug quá dài (tối đa 120 ký tự)." });

            var existsName = await _context.GameCategories.AnyAsync(x => x.Name == name);
            if (existsName)
                return BadRequest(new { success = false, message = "Tên danh mục đã tồn tại." });

            var existsSlug = await _context.GameCategories.AnyAsync(x => x.Slug == slug);
            if (existsSlug)
                return BadRequest(new { success = false, message = "Slug đã tồn tại." });

            var entity = new GameCategory
            {
                Name = name,
                Slug = slug,
                CreatedAt = DateTime.UtcNow
            };

            _context.GameCategories.Add(entity);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã thêm danh mục.",
                data = new
                {
                    entity.CategoryId,
                    entity.Name,
                    entity.Slug,
                    CreatedAt = entity.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    GameCount = 0
                }
            });
        }

        public class CategoryUpdateJson { public int CategoryId { get; set; } public string? Name { get; set; } public string? Slug { get; set; } }

        [HttpPost]
        [Route("/admin/api/category/update")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiCategoryUpdate([FromBody] CategoryUpdateJson req)
        {
            var guard = RequireAdminJson();
            if (guard != null) return guard;

            if (req == null || req.CategoryId <= 0)
                return BadRequest(new { success = false, message = "CategoryId không hợp lệ." });

            var entity = await _context.GameCategories.FirstOrDefaultAsync(x => x.CategoryId == req.CategoryId);
            if (entity == null)
                return NotFound(new { success = false, message = "Không tìm thấy danh mục." });

            var name = (req.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 80)
                return BadRequest(new { success = false, message = "Tên danh mục không hợp lệ (1-80 ký tự)." });

            var slug = (req.Slug ?? "").Trim();
            if (string.IsNullOrWhiteSpace(slug))
                slug = Slugify(name);
            if (slug.Length > 120)
                return BadRequest(new { success = false, message = "Slug quá dài (tối đa 120 ký tự)." });

            var existsName = await _context.GameCategories.AnyAsync(x => x.Name == name && x.CategoryId != entity.CategoryId);
            if (existsName)
                return BadRequest(new { success = false, message = "Tên danh mục đã tồn tại." });

            var existsSlug = await _context.GameCategories.AnyAsync(x => x.Slug == slug && x.CategoryId != entity.CategoryId);
            if (existsSlug)
                return BadRequest(new { success = false, message = "Slug đã tồn tại." });

            entity.Name = name;
            entity.Slug = slug;

            await _context.SaveChangesAsync();

            var gameCount = await _context.Games.CountAsync(g => g.CategoryId == entity.CategoryId);

            return Json(new
            {
                success = true,
                message = "Đã cập nhật danh mục.",
                data = new
                {
                    entity.CategoryId,
                    entity.Name,
                    entity.Slug,
                    CreatedAt = entity.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    GameCount = gameCount
                }
            });
        }

        public class CategoryDeleteJson { public int CategoryId { get; set; } }

        [HttpPost]
        [Route("/admin/api/category/delete")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiCategoryDelete([FromBody] CategoryDeleteJson req)
        {
            var guard = RequireAdminJson();
            if (guard != null) return guard;

            if (req == null || req.CategoryId <= 0)
                return BadRequest(new { success = false, message = "CategoryId không hợp lệ." });

            var entity = await _context.GameCategories
                .Include(x => x.Games)
                .FirstOrDefaultAsync(x => x.CategoryId == req.CategoryId);

            if (entity == null)
                return NotFound(new { success = false, message = "Không tìm thấy danh mục." });

            if (entity.Games.Any())
                return BadRequest(new { success = false, message = "Danh mục đang có game, không thể xóa." });

            _context.GameCategories.Remove(entity);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa danh mục." });
        }

        // =========================
        // Helper: slugify
        // =========================
        private static string Slugify(string input)
        {
            input = (input ?? "").Trim().ToLowerInvariant();
            input = input.Replace("đ", "d").Normalize(NormalizationForm.FormD);

            var chars = input.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            input = new string(chars).Normalize(NormalizationForm.FormC);

            input = Regex.Replace(input, @"[^a-z0-9]+", "-");
            input = Regex.Replace(input, @"-+", "-").Trim('-');

            return string.IsNullOrWhiteSpace(input) ? "category" : input;
        }

        // =========================
        // VIEW: Game (Partial)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Game()
        {
            var guard = RequireAdminView();
            if (guard != null) return guard;

            var categories = await _context.GameCategories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new { x.CategoryId, x.Name })
                .ToListAsync();

            ViewBag.Categories = categories;

            var list = await _context.Games
                .AsNoTracking()
                .Include(x => x.Category)
                .OrderByDescending(x => x.GameId)
                .Select(x => new GameRowVm
                {
                    GameId = x.GameId,
                    CategoryId = x.CategoryId,
                    CategoryName = x.Category.Name,
                    Name = x.Name,
                    Slug = x.Slug,
                    ThumbnailUrl = x.ThumbnailUrl,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    ListingCount = x.AccountListings.Count
                })
                .ToListAsync();

            return PartialView("Partials/Admin/_Game", list);
        }

        [HttpGet]
        [Route("/admin/api/game/list")]
        public async Task<IActionResult> ApiGameList()
        {
            var guard = RequireAdminJson();
            if (guard != null) return guard;

            var list = await _context.Games
                .AsNoTracking()
                .Include(x => x.Category)
                .OrderByDescending(x => x.GameId)
                .Select(x => new
                {
                    x.GameId,
                    x.CategoryId,
                    CategoryName = x.Category.Name,
                    x.Name,
                    x.Slug,
                    x.Description,
                    x.ThumbnailUrl,
                    x.IsActive,
                    CreatedAt = x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    ListingCount = x.AccountListings.Count
                })
                .ToListAsync();

            return Json(new { success = true, data = list });
        }

        // ... (phần GameCreate / GameUpdate / GameDelete của bạn)
        // Bạn chỉ cần thêm guard RequireAdminJson() y như Category API ở trên vào các API đó.
    }
}
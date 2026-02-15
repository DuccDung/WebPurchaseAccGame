using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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

        public async Task<IActionResult> Dashboard()
        {
            // if (!IsAdmin()) return Unauthorized();

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            // Doanh thu tháng + số đơn PAID tháng
            var paidThisMonth = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == "PAID" && o.CreatedAt >= monthStart)
                .ToListAsync();

            ViewBag.RevenueMonth = paidThisMonth.Sum(x => x.TotalAmount);
            ViewBag.SoldCountMonth = paidThisMonth.Count;

            // Topup PENDING (đổ vào bảng, lấy 10 cái đầu)
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

            // Recent PAID Orders (top 5)
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
        // API: Approve / Reject (JSON)
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
        [IgnoreAntiforgeryToken] // nếu bạn muốn dùng token thì bỏ dòng này và gửi token từ JS
        public async Task<IActionResult> ApiApproveTopup([FromBody] ApproveTopupJson req)
        {
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
            // if (!IsAdmin()) return Unauthorized();

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

        // =========================
        // API: LIST
        // =========================
        [HttpGet]
        [Route("/admin/api/category/list")]
        public async Task<IActionResult> ApiCategoryList()
        {
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

        // =========================
        // API: CREATE
        // =========================
        public class CategoryCreateJson
        {
            public string? Name { get; set; }
            public string? Slug { get; set; } // optional
        }

        [HttpPost]
        [Route("/admin/api/category/create")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiCategoryCreate([FromBody] CategoryCreateJson req)
        {
            var name = (req?.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 80)
                return BadRequest(new { success = false, message = "Tên danh mục không hợp lệ (1-80 ký tự)." });

            var slug = (req?.Slug ?? "").Trim();
            if (string.IsNullOrWhiteSpace(slug))
                slug = Slugify(name);
            if (slug.Length > 120)
                return BadRequest(new { success = false, message = "Slug quá dài (tối đa 120 ký tự)." });

            // unique check
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

        // =========================
        // API: UPDATE
        // =========================
        public class CategoryUpdateJson
        {
            public int CategoryId { get; set; }
            public string? Name { get; set; }
            public string? Slug { get; set; } // optional
        }

        [HttpPost]
        [Route("/admin/api/category/update")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiCategoryUpdate([FromBody] CategoryUpdateJson req)
        {
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

            // unique check (exclude self)
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

        // =========================
        // API: DELETE
        // =========================
        public class CategoryDeleteJson
        {
            public int CategoryId { get; set; }
        }

        [HttpPost]
        [Route("/admin/api/category/delete")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiCategoryDelete([FromBody] CategoryDeleteJson req)
        {
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

            // bỏ dấu tiếng việt cơ bản
            input = input
                .Replace("đ", "d")
                .Normalize(NormalizationForm.FormD);

            var chars = input.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            input = new string(chars).Normalize(NormalizationForm.FormC);

            // ký tự không hợp lệ -> -
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
            // if (!IsAdmin()) return Unauthorized();

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

        // =========================
        // API: LIST
        // =========================
        [HttpGet]
        [Route("/admin/api/game/list")]
        public async Task<IActionResult> ApiGameList()
        {
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

        // =========================
        // API: CREATE
        // =========================
        public class GameCreateJson
        {
            public int CategoryId { get; set; }
            public string? Name { get; set; }
            public string? Slug { get; set; } // optional
            public string? Description { get; set; }
            public string? ThumbnailUrl { get; set; }
            public bool IsActive { get; set; } = true;
        }

        [HttpPost]
        [Route("/admin/api/game/create")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiGameCreate([FromBody] GameCreateJson req)
        {
            if (req == null) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            if (req.CategoryId <= 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn Category." });

            var catOk = await _context.GameCategories.AnyAsync(x => x.CategoryId == req.CategoryId);
            if (!catOk)
                return BadRequest(new { success = false, message = "Category không tồn tại." });

            var name = (req.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
                return BadRequest(new { success = false, message = "Tên game không hợp lệ (1-120 ký tự)." });

            var slug = (req.Slug ?? "").Trim();
            if (string.IsNullOrWhiteSpace(slug)) slug = Slugify(name);
            if (slug.Length > 160)
                return BadRequest(new { success = false, message = "Slug quá dài (tối đa 160 ký tự)." });

            // unique slug
            var existsSlug = await _context.Games.AnyAsync(x => x.Slug == slug);
            if (existsSlug)
                return BadRequest(new { success = false, message = "Slug đã tồn tại." });

            var thumb = (req.ThumbnailUrl ?? "").Trim();
            if (thumb.Length > 500) return BadRequest(new { success = false, message = "ThumbnailUrl quá dài (tối đa 500)." });

            var entity = new Game
            {
                CategoryId = req.CategoryId,
                Name = name,
                Slug = slug,
                Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
                ThumbnailUrl = string.IsNullOrWhiteSpace(thumb) ? null : thumb,
                IsActive = req.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Games.Add(entity);
            await _context.SaveChangesAsync();

            var categoryName = await _context.GameCategories
                .Where(x => x.CategoryId == entity.CategoryId)
                .Select(x => x.Name)
                .FirstAsync();

            return Json(new
            {
                success = true,
                message = "Đã thêm game.",
                data = new
                {
                    entity.GameId,
                    entity.CategoryId,
                    CategoryName = categoryName,
                    entity.Name,
                    entity.Slug,
                    entity.Description,
                    entity.ThumbnailUrl,
                    entity.IsActive,
                    CreatedAt = entity.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    ListingCount = 0
                }
            });
        }

        // =========================
        // API: UPDATE
        // =========================
        public class GameUpdateJson
        {
            public int GameId { get; set; }
            public int CategoryId { get; set; }
            public string? Name { get; set; }
            public string? Slug { get; set; } // optional
            public string? Description { get; set; }
            public string? ThumbnailUrl { get; set; }
            public bool IsActive { get; set; } = true;
        }

        [HttpPost]
        [Route("/admin/api/game/update")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiGameUpdate([FromBody] GameUpdateJson req)
        {
            if (req == null || req.GameId <= 0)
                return BadRequest(new { success = false, message = "GameId không hợp lệ." });

            var entity = await _context.Games.FirstOrDefaultAsync(x => x.GameId == req.GameId);
            if (entity == null)
                return NotFound(new { success = false, message = "Không tìm thấy game." });

            if (req.CategoryId <= 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn Category." });

            var catOk = await _context.GameCategories.AnyAsync(x => x.CategoryId == req.CategoryId);
            if (!catOk)
                return BadRequest(new { success = false, message = "Category không tồn tại." });

            var name = (req.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
                return BadRequest(new { success = false, message = "Tên game không hợp lệ (1-120 ký tự)." });

            var slug = (req.Slug ?? "").Trim();
            if (string.IsNullOrWhiteSpace(slug)) slug = Slugify(name);
            if (slug.Length > 160)
                return BadRequest(new { success = false, message = "Slug quá dài (tối đa 160 ký tự)." });

            // unique slug exclude self
            var existsSlug = await _context.Games.AnyAsync(x => x.Slug == slug && x.GameId != entity.GameId);
            if (existsSlug)
                return BadRequest(new { success = false, message = "Slug đã tồn tại." });

            var thumb = (req.ThumbnailUrl ?? "").Trim();
            if (thumb.Length > 500) return BadRequest(new { success = false, message = "ThumbnailUrl quá dài (tối đa 500)." });

            entity.CategoryId = req.CategoryId;
            entity.Name = name;
            entity.Slug = slug;
            entity.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            entity.ThumbnailUrl = string.IsNullOrWhiteSpace(thumb) ? null : thumb;
            entity.IsActive = req.IsActive;

            await _context.SaveChangesAsync();

            var categoryName = await _context.GameCategories
                .Where(x => x.CategoryId == entity.CategoryId)
                .Select(x => x.Name)
                .FirstAsync();

            var listingCount = await _context.AccountListings.CountAsync(a => a.GameId == entity.GameId);

            return Json(new
            {
                success = true,
                message = "Đã cập nhật game.",
                data = new
                {
                    entity.GameId,
                    entity.CategoryId,
                    CategoryName = categoryName,
                    entity.Name,
                    entity.Slug,
                    entity.Description,
                    entity.ThumbnailUrl,
                    entity.IsActive,
                    CreatedAt = entity.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    ListingCount = listingCount
                }
            });
        }

        // =========================
        // API: DELETE
        // =========================
        public class GameDeleteJson { public int GameId { get; set; } }

        [HttpPost]
        [Route("/admin/api/game/delete")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiGameDelete([FromBody] GameDeleteJson req)
        {
            if (req == null || req.GameId <= 0)
                return BadRequest(new { success = false, message = "GameId không hợp lệ." });

            var entity = await _context.Games
                .Include(x => x.AccountListings)
                .FirstOrDefaultAsync(x => x.GameId == req.GameId);

            if (entity == null)
                return NotFound(new { success = false, message = "Không tìm thấy game." });

            if (entity.AccountListings.Any())
                return BadRequest(new { success = false, message = "Game đang có account listing, không thể xóa." });

            _context.Games.Remove(entity);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa game." });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemPurchaseAccGame.Models;
using SystemPurchaseAccGame.Models.ViewModels;

namespace SystemPurchaseAccGame.Controllers.Admin
{
    public class LuckyAdminController : Controller
    {
        private readonly GameAccShopContext _db;
        private readonly IWebHostEnvironment _env;

        // ====== Session Keys (PHẢI TRÙNG với AdminController) ======
        private const string S_ADMIN_ID = "ADMIN_ID";
        private const string S_ADMIN_ROLE = "ADMIN_ROLE";

        public LuckyAdminController(GameAccShopContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ====== CHECK LOGIN ======
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString(S_ADMIN_ROLE);
            var id = HttpContext.Session.GetInt32(S_ADMIN_ID);
            return id.HasValue && id.Value > 0 && string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult RequireAdmin()
        {
            if (IsAdmin()) return null!;
            return RedirectToAction("Login", "Admin"); // <<<< về /Admin/Login
        }

        public async Task<IActionResult> Index()
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            var items = await _db.LuckySpinItems
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.PrizeValue)
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Edit(long id)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            var item = await _db.LuckySpinItems.AsNoTracking().FirstOrDefaultAsync(x => x.ItemId == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, LuckySpinItem model)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            if (id != model.ItemId) return BadRequest();

            try
            {
                model.UpdatedAt = DateTime.UtcNow;
                _db.Entry(model).Property(x => x.RowVer).OriginalValue = model.RowVer;
                _db.Update(model);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Dữ liệu đã bị người khác chỉnh sửa. Vui lòng tải lại.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetWinner(long id)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            var item = await _db.LuckySpinItems.FirstOrDefaultAsync(x => x.ItemId == id);
            if (item == null) return NotFound();

            item.LastWinnerUserId = null;
            item.LastWonAt = null;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ================== PHẦN ẢNH VÒNG QUAY (MỚI - KHÔNG DB) ==================

        // Bạn có thể đổi folder nếu muốn: wwwroot/img/lucky/
        private string LuckyFolderAbs => Path.Combine(_env.WebRootPath, "img", "lucky");
        private string LuckyFolderUrl => "/img/lucky";

        // Các extension cho phép
        private static readonly string[] AllowedExts = new[] { ".png", ".jpg", ".jpeg", ".webp" };

        // Trang quản lý 9 ảnh
        public IActionResult LuckyImages()
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            Directory.CreateDirectory(LuckyFolderAbs);

            var vm = new LuckyImageIndexVm();

            for (int slot = 1; slot <= 9; slot++)
            {
                var key = $"ID_{slot:00}";
                var url = FindExistingImageUrl(key);

                vm.Slots.Add(new LuckyImageSlotVm
                {
                    Slot = slot,
                    FileKey = key,
                    Url = url
                });
            }

            return View(vm);
        }

        // Upload/Replace ảnh cho 1 slot
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLuckyImage(int slot, IFormFile file)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            if (slot < 1 || slot > 9) return BadRequest("Slot chỉ từ 1..9.");
            if (file == null || file.Length == 0)
            {
                TempData["err"] = "Bạn chưa chọn file ảnh.";
                return RedirectToAction(nameof(LuckyImages));
            }

            Directory.CreateDirectory(LuckyFolderAbs);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Validate extension
            if (!AllowedExts.Contains(ext))
            {
                TempData["err"] = "Chỉ cho phép ảnh: .png, .jpg, .jpeg, .webp";
                return RedirectToAction(nameof(LuckyImages));
            }

            // Validate content type (nhẹ)
            if (file.ContentType == null || !file.ContentType.StartsWith("image/"))
            {
                TempData["err"] = "File không phải ảnh hợp lệ.";
                return RedirectToAction(nameof(LuckyImages));
            }

            var key = $"ID_{slot:00}";

            // Xoá mọi file cũ của slot (ID_01.*)
            DeleteAllVariants(key);

            // Lưu file mới theo đúng tên cố định ID_0X + ext
            var fileName = key + ext;
            var absPath = Path.Combine(LuckyFolderAbs, fileName);

            using (var stream = System.IO.File.Create(absPath))
            {
                await file.CopyToAsync(stream);
            }

            TempData["ok"] = $"Đã cập nhật ảnh vị trí #{slot} ({fileName}).";
            return RedirectToAction(nameof(LuckyImages));
        }

        // Xoá ảnh của 1 slot
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteLuckyImage(int slot)
        {
            var guard = RequireAdmin();
            if (guard != null) return guard;

            if (slot < 1 || slot > 9) return BadRequest("Slot chỉ từ 1..9.");

            Directory.CreateDirectory(LuckyFolderAbs);

            var key = $"ID_{slot:00}";
            DeleteAllVariants(key);

            TempData["ok"] = $"Đã xoá ảnh vị trí #{slot}.";
            return RedirectToAction(nameof(LuckyImages));
        }

        // ================== HELPERS ==================

        // Tìm URL ảnh đang tồn tại cho key (ưu tiên theo thứ tự ext)
        private string? FindExistingImageUrl(string key)
        {
            foreach (var ext in AllowedExts)
            {
                var abs = Path.Combine(LuckyFolderAbs, key + ext);
                if (System.IO.File.Exists(abs))
                    return $"{LuckyFolderUrl}/{key}{ext}";
            }
            return null;
        }

        // Xoá tất cả biến thể ID_01.png/jpg/jpeg/webp
        private void DeleteAllVariants(string key)
        {
            foreach (var ext in AllowedExts)
            {
                var abs = Path.Combine(LuckyFolderAbs, key + ext);
                if (System.IO.File.Exists(abs))
                    System.IO.File.Delete(abs);
            }
        }
    }
}
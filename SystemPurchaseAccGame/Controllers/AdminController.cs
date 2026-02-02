using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemPurchaseAccGame.Models;

namespace SystemPurchaseAccGame.Controllers
{
    public class AdminController : Controller
    {
        private readonly GameAccShopContext _context;
        public AdminController(GameAccShopContext context)
        {
            _context = context;
        }
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
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SystemPurchaseAccGame.Models;
using SystemPurchaseAccGame.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Claims;
public class HomeController : Controller
{
    private readonly GameAccShopContext _context;

    public HomeController(GameAccShopContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Login()
    {
        await Task.CompletedTask;
        return View();
    }


    // POST: /Home/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model)
    {
        bool status = false;

        var user = await _context.Users
             .FirstOrDefaultAsync(u => (u.Email == model.Identity || u.Phone == model.Identity) && u.PasswordHash == model.Password);
        if (user != null) status = true;
        if (status && user != null)
        {
            var claims = new List<Claim>
                {
                  new Claim(ClaimTypes.NameIdentifier , user.UserId.ToString()),
                  new Claim(ClaimTypes.Name , user.Email ?? ""),
                };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            if (model.Remember)
            {
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    });
            }
            else await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("HomePage", "Home");
        }

        // thất bại: báo view
        ViewBag.LoginError = "Email/Tài khoản hoặc mật khẩu không đúng.";
        ViewBag.ActiveTab = "login";
        return View();
    }

    // POST: /Home/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        bool status = true;
        var user = new User
        {
            Email = model.Email,
            Phone = model.Phone,
            FullName = model.Name,
            Username = model.Name,
            PasswordHash = model.Password
        };
        try {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }
        catch
        {
            status = false;
        }
        if (status)
        {
            TempData["RegisterSuccess"] = "Tạo tài khoản thành công. Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        ViewBag.RegisterError = "Đăng ký không thành công. Vui lòng kiểm tra lại thông tin.";
        ViewBag.ActiveTab = "register";
        return View("Login"); 
    }
   
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        Response.Cookies.Delete("my_cookie");

        return RedirectToAction("Login", "Home");
    }
    public async Task<IActionResult> HomePage()
    {
        ViewBag.IsAuthenticated = User?.Identity?.IsAuthenticated == true;

        if (ViewBag.IsAuthenticated)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
            {
                ViewBag.Email = User.FindFirstValue(ClaimTypes.Name);

                var u = await _context.Users
                    .AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .Select(x => new { x.UserId, x.Username })
                    .FirstOrDefaultAsync();

                if (u != null)
                {
                    ViewBag.UserId = u.UserId;
                    ViewBag.AccountName = u.Username;
                }
            }
        }


        var result = await _context.GameCategories.Include(c => c.Games)

            .Select(c => new GameCategoryVm
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Slug = c.Slug,
                Games = c.Games.Select(g => new GameStatsVm
                {
                    GameId = g.GameId,
                    Name = g.Name,
                    Slug = g.Slug,
                    ThumbnailUrl = g.ThumbnailUrl,
                    SoldCount = g.AccountListings.Count(al => al.Status == "SOLD"),
                    RemainingCount = g.AccountListings.Count(al => al.Status == "AVAILABLE")
                }).ToList()
            })
            .ToListAsync();
        return View(result);
    }
    public IActionResult GameDetail(int id)
    {
        var accounts = _context.AccountListings
            .Where(al => al.GameId == id && al.Status == "AVAILABLE")
            .Select(al => new AccountListingVm
            {
                AccountListingId = al.AccountId,
                Title = al.Title,
                Description = al.Description ?? "",
                urlPhoto = al.AccountMedia
                    .Where(x => x.MediaType == "thumbnail")
                    .Select(x => x.Url)
                    .FirstOrDefault() ?? string.Empty,
                Price = al.Price / 100m
            })
            .ToList();
        return View(accounts);
    }
}

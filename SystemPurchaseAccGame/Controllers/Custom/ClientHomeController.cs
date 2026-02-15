using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SystemPurchaseAccGame.Models;
using SystemPurchaseAccGame.ViewModel;

namespace SystemPurchaseAccGame.Controllers.Custom
{
    public class ClientHomeController : Controller
    {
        private readonly GameAccShopContext _context;
        public ClientHomeController(GameAccShopContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
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
                        Price = g.Price,
                        SoldCount = g.AccountListings.Count(al => al.Status == "SOLD"),
                        RemainingCount = g.AccountListings.Count(al => al.Status == "AVAILABLE")
                    }).ToList()
                })
                .ToListAsync();
            return View(result);
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
                var balance = await _context.Wallets
                           .Where(x => x.UserId == user.UserId)
                           .Select(x => x.Balance)
                           .FirstOrDefaultAsync();

                var claims = new List<Claim>
                {
                  new Claim(ClaimTypes.NameIdentifier , user.UserId.ToString()),
                  new Claim(ClaimTypes.Name , user.Email ?? ""),
                    new Claim("balance", balance.ToString(System.Globalization.CultureInfo.InvariantCulture))
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

                ViewBag.Balance = await _context.Wallets.Where(x => x.UserId == user.UserId).Select(x => x.Balance).FirstOrDefaultAsync();

                return RedirectToAction("Index", "ClientHome");
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
            try
            {
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

        [HttpGet]
        public async Task<IActionResult> Purchase(long id) // id = AccountId
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
            var acc = await _context.AccountListings
                .AsNoTracking()
                .Include(x => x.AccountMedia)
                .Include(x => x.AccountAttributes)
                .FirstOrDefaultAsync(x => x.AccountId == id);

            if (acc == null) return NotFound();

            var vm = new AccountPurchaseDto
            {
                AccountId = acc.AccountId,
                GameId = acc.GameId,
                Title = acc.Title,
                Price = acc.Price,
                Description = acc.Description,
                Status = acc.Status,

                Media = acc.AccountMedia
                    .OrderBy(m => m.SortOrder)
                    .Select(m => new MediaDto
                    {
                        MediaType = MapMediaType(m.MediaType),  // background/thumbnail/gallery -> COVER/AVATAR/GALLERY
                        Url = NormalizeUrl(m.Url),
                        SortOrder = m.SortOrder
                    })
                    .ToList(),

                Attributes = acc.AccountAttributes
                    .OrderBy(a => a.AttrKey)
                    .Select(a => new AttrDto
                    {
                        Key = a.AttrKey,
                        Value = a.AttrValue
                    })
                    .ToList()
            };

            return View(vm);
        }
        public async Task<IActionResult> GameDetail(int id)
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

            // 1) Lấy list account trước
            var accounts = await _context.AccountListings
                .AsNoTracking()
                .Where(al => al.GameId == id && al.Status == "AVAILABLE")
                .Select(al => new AccountListingVm
                {
                    AccountListingId = al.AccountId,
                    Title = al.Title,
                    Description = al.Description ?? "",
                    urlPhoto = al.AccountMedia
                        .Where(m => m.MediaType == "thumbnail")
                        .Select(m => m.Url)
                        .FirstOrDefault() ?? "",
                    Price = al.Price
                })
                .ToListAsync();

            // 2) Lấy attributes theo list AccountId (1 query)
            var ids = accounts.Select(x => x.AccountListingId).ToList();

            var attrs = await _context.AccountAttributes
                .AsNoTracking()
                .Where(a => ids.Contains(a.AccountId))
                .Select(a => new
                {
                    a.AccountId,
                    a.AttrKey,
                    a.AttrValue,
                })
                .ToListAsync();

            // 3) Group vào dictionary
            var dict = attrs
                .GroupBy(x => x.AccountId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .Select(x => new AttrDto { Key = x.AttrKey, Value = x.AttrValue })
                        .ToList()
                );

            // 4) gán vào VM
            foreach (var acc in accounts)
            {
                if (dict.TryGetValue(acc.AccountListingId, out var list))
                    acc.Attributes = list;
            }

            return View(accounts);
        }
        private string MapMediaType(string? raw)
        {
            var t = (raw ?? "").Trim().ToLowerInvariant();
            return t switch
            {
                "background" => "COVER",
                "thumbnail" => "AVATAR",
                "gallery" => "GALLERY",
                _ => t.ToUpperInvariant()
            };
        }

        private string NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";

            url = url.Trim();

            // url tuyệt đối
            if (url.StartsWith("http://") || url.StartsWith("https://"))
                return url;

            // nếu DB lưu "/img/xxx.jpg" hoặc "img/xxx.jpg"
            if (url.StartsWith("/"))
                return Url.Content("~" + url);      // => "~/img/.."
            if (url.StartsWith("~/"))
                return Url.Content(url);

            return Url.Content("~/" + url);         // => "~/img/.."
        }
    }
}

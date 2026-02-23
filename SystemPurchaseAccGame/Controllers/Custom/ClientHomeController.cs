using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
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

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(SystemPurchaseAccGame.Dtos.RegisterVm model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.RegisterError = "Vui lòng nhập đầy đủ thông tin.";
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.RegisterError = "Mật khẩu xác nhận không khớp.";
                return View(model);
            }

            var existed = await _context.Users
                .AnyAsync(x => x.Email == model.Email);
            var existedPhone = await _context.Users
                .AnyAsync(x => x.Phone == model.Phone);
            if (existedPhone)
            {
                ViewBag.RegisterError = "Số điện thoại đã tồn tại.";
                return View(model);
            }
            if (existed)
            {
                ViewBag.RegisterError = "Email đã tồn tại.";
                return View(model);
            }

            var user = new User
            {
                Username = "" + model.Email.Split('@')[0], // Lấy phần trước dấu @ làm username tạm
                Email = model.Email,
                Phone = model.Phone,
                PasswordHash = model.Password, // ⚠️ sau này nên hash
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var existedWallet = await _context.Wallets
     .AnyAsync(x => x.UserId == user.UserId);

            if (!existedWallet)
            {
                var wallet = new Wallet
                {
                    UserId = user.UserId,
                    Balance = 0,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            // Auto login sau khi đăng ký
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier , user.UserId.ToString()),
        new Claim(ClaimTypes.Name , user.Email),
        new Claim("balance", "0")
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            return RedirectToAction("Index", "ClientHome");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            Response.Cookies.Delete("my_cookie");

            return RedirectToAction("Login", "ClientHome");
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
        public async Task<IActionResult> OrderHistory()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Login");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login");

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Account)
                .ToListAsync();

            var result = new List<OrderHistoryVm>();

            foreach (var order in orders)
            {
                var vm = new OrderHistoryVm
                {
                    OrderId = order.OrderId,
                    CreatedAt = order.CreatedAt,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status
                };

                foreach (var item in order.OrderItems)
                {
                    var acc = item.Account;

                    string username = "";
                    string password = "";
                    string? note = null;

                    if (!string.IsNullOrWhiteSpace(acc?.LoginInfo))
                    {
                        try
                        {
                            var li = JsonSerializer.Deserialize<LoginInfoJson>(acc.LoginInfo);
                            username = li?.User ?? "";
                            password = li?.Pass ?? "";
                            note = li?.Note;
                        }
                        catch { }
                    }

                    vm.Items.Add(new OrderHistoryItemVm
                    {
                        AccountId = acc?.AccountId ?? 0,
                        Title = acc?.Title ?? "",
                        Price = item.UnitPrice,
                        Username = username,
                        Password = password,
                        Note = note
                    });
                }

                result.Add(vm);
            }

            return View(result);
        }
        public async Task<IActionResult> Bank()
        {
            ViewBag.IsAuthenticated = User?.Identity?.IsAuthenticated == true;

            if (!ViewBag.IsAuthenticated)
            {
                return RedirectToAction("Login", "ClientHome");
            }
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
            {
                var wallet = await _context.Wallets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.UserId == userId);
                ViewBag.WalletBalance = wallet != null ? wallet.Balance : 0m;
            }
            await Task.CompletedTask;
            return View();
        }
        public async Task<IActionResult> PaymentSuccess()
        {
            // bắt buộc đăng nhập
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Login", "ClientHome");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out var userId))
                return RedirectToAction("Login", "ClientHome");

            // ====== CỘNG 1 LƯỢT QUAY ======
            // Key theo từng user để tránh user khác dùng chung
            var key = $"LUCKY_SPIN_CHANCE_{userId}";

            // Nếu bạn muốn "mỗi lần mua chỉ 1 lượt và không cộng dồn", set = 1 luôn
            // HttpContext.Session.SetInt32(key, 1);

            // Nếu bạn muốn "mua nhiều lần cộng dồn lượt quay"
            var current = HttpContext.Session.GetInt32(key) ?? 0;
            HttpContext.Session.SetInt32(key, current + 1);

            // (Tuỳ chọn) đưa cờ để home hiển thị nút "Quay ngay"
            TempData["LuckySpinGranted"] = true;

            await Task.CompletedTask;
            return View();
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

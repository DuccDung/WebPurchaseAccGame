using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SystemPurchaseAccGame.Dtos;
using SystemPurchaseAccGame.Models;
using SystemPurchaseAccGame.ViewModel;
using static System.Runtime.InteropServices.JavaScript.JSType;
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
    public async Task<IActionResult> Bank()
    {
        ViewBag.IsAuthenticated = User?.Identity?.IsAuthenticated == true;

        if (!ViewBag.IsAuthenticated)
        {
            return RedirectToAction("Login", "Home");
        }
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var userId))
        { 
            var wallet = await _context.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId);
            ViewBag.WalletBalance = wallet != null ? wallet.Balance / 100m : 0m;
        }
            await Task.CompletedTask;
        return View();
    }
    public async Task<IActionResult> PaymentSuccess()
    {
        await Task.CompletedTask;
        return View();
    }

    private static string MakeTxnCode()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // ms
        var rnd = Random.Shared.Next(1000, 10000); // 4 digits
        return $"g{ts}{rnd}";
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
    public async Task<IActionResult> Cart()
    {
        // ==== bạn giữ phần ViewBag auth như bạn đang làm ====
        ViewBag.IsAuthenticated = User?.Identity?.IsAuthenticated == true;
        if (!ViewBag.IsAuthenticated)
            return RedirectToAction("Login", "Home");

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
            return RedirectToAction("Login", "Home");

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

        // ==== load cart (Order PENDING) ====
        var cartOrder = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.Status == "PENDING")
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.OrderId,
                o.Status,
                o.PaymentMethod,
                Items = o.OrderItems.Select(oi => new
                {
                    oi.OrderItemId,
                    oi.AccountId,
                    oi.UnitPrice,
                    AccountTitle = oi.Account.Title,
                    AccountStatus = oi.Account.Status,
                    Thumb = oi.Account.AccountMedia
                        .Where(m => m.MediaType == "thumbnail")
                        .OrderBy(m => m.SortOrder)
                        .Select(m => m.Url)
                        .FirstOrDefault()
                }).ToList()
            })
            .FirstOrDefaultAsync();

        // ==== build VM ====
        var vm = new CartVm();

        if (cartOrder == null)
        {
            // giỏ trống
            return View(vm);
        }

        vm.OrderId = cartOrder.OrderId;
        vm.Status = cartOrder.Status;
        vm.PaymentMethod = cartOrder.PaymentMethod;

        foreach (var it in cartOrder.Items)
        {
            vm.Items.Add(new CartItemVm
            {
                OrderItemId = it.OrderItemId,
                AccountId = it.AccountId,
                UnitPrice = it.UnitPrice,
                Title = it.AccountTitle ?? "",
                Status = it.AccountStatus ?? "",
                ThumbnailUrl = NormalizeUrl(it.Thumb) // dùng lại NormalizeUrl của bạn
            });
        }

        vm.Subtotal = vm.Items.Sum(x => x.UnitPrice);
        vm.Fee = 0;

        return View(vm);
    }


    [HttpPost]
    [Route("api/cart/add/{accountId:long}")]
    public async Task<IActionResult> ApiAddToCart(long accountId)
    {
        // 1) lấy userId từ cookie auth
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new ApiResult { Ok = false, Message = "Bạn chưa đăng nhập." });
        }

        // 2) check account còn bán được
        var acc = await _context.AccountListings
            .AsNoTracking()
            .Where(a => a.AccountId == accountId)
            .Select(a => new { a.AccountId, a.Price, a.Status })
            .FirstOrDefaultAsync();

        if (acc == null)
            return NotFound(new ApiResult { Ok = false, Message = "Account không tồn tại." });

        if (!string.Equals(acc.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ApiResult { Ok = false, Message = "Account hiện không khả dụng." });

        // 3) dùng transaction để tránh race condition
        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            // 4) tìm giỏ hàng (order PENDING) hiện có của user
            var cart = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "PENDING");

            // 5) nếu chưa có, tạo mới
            if (cart == null)
            {
                cart = new Order
                {
                    UserId = userId,
                    Status = "PENDING",          // đúng theo CHECK constraint
                    PaymentMethod = "WALLET",    // tạm set 1 cái hợp lệ (khi checkout bạn update lại)
                    TotalAmount = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(cart);
                await _context.SaveChangesAsync(); // để có OrderId
            }

            // 6) check item đã có trong cart chưa
            var existsInCart = await _context.OrderItems
                .AnyAsync(oi => oi.OrderId == cart.OrderId && oi.AccountId == accountId);

            if (existsInCart)
            {
                await tx.CommitAsync();
                return Ok(new ApiResult
                {
                    Ok = true,
                    Message = "Account đã có trong giỏ hàng.",
                    Data = new { cartId = cart.OrderId }
                });
            }

            // 7) check account đã bị add vào order khác chưa (do unique UQ_OrderItems_Account)
            var existsElsewhere = await _context.OrderItems
                .AnyAsync(oi => oi.AccountId == accountId);

            if (existsElsewhere)
            {
                await tx.RollbackAsync();
                return Conflict(new ApiResult
                {
                    Ok = false,
                    Message = "Account đã nằm trong đơn hàng khác (có thể đã được người khác thêm/mua)."
                });
            }

            // 8) add OrderItem
            var item = new OrderItem
            {
                OrderId = cart.OrderId,
                AccountId = accountId,
                UnitPrice = acc.Price
            };
            _context.OrderItems.Add(item);

            // 9) update total
            cart.TotalAmount = cart.TotalAmount + acc.Price;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new ApiResult
            {
                Ok = true,
                Message = "Đã thêm vào giỏ hàng.",
                Data = new
                {
                    cartId = cart.OrderId,
                    total = cart.TotalAmount,
                    addedAccountId = accountId
                }
            });
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            // thường dính unique constraint UQ_OrderItems_Account
            return Conflict(new ApiResult
            {
                Ok = false,
                Message = "Không thể thêm account (có thể đã được thêm/mua bởi người khác)."
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new ApiResult { Ok = false, Message = "Lỗi hệ thống: " + ex.Message });
        }
    }
    public async Task<IActionResult> HistoryPayment()
    {
        ViewBag.IsAuthenticated = User?.Identity?.IsAuthenticated == true;

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
            return RedirectToAction("Login", "Auth");

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

        // ===== Topup history =====
        var topups = await _context.Topups
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TopupRowVm
            {
                TopupId = t.TopupId,
                Method = t.Method,
                Amount = t.Amount,
                Fee = t.Fee,
                Status = t.Status,
                Provider = t.Provider,
                ReferenceCode = t.ReferenceCode,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt
            })
            .ToListAsync();

        // ===== Order history (kèm title account) =====
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderRowVm
            {
                OrderId = o.OrderId,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentMethod = o.PaymentMethod,
                CreatedAt = o.CreatedAt,
                PaidAt = o.PaidAt,
                Items = o.OrderItems.Select(oi => new OrderItemRowVm
                {
                    AccountId = oi.AccountId,
                    UnitPrice = oi.UnitPrice,
                    Title = oi.Account.Title
                }).ToList()
            })
            .ToListAsync();

        var vm = new PaymentHistoryVm
        {
            Topups = topups,
            Orders = orders
        };

        return View(vm);
    }

    [HttpPost]
    [Route("api/cart/remove/{accountId:long}")]
    public async Task<IActionResult> ApiCartRemove(long accountId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
            return Unauthorized(new ApiResult { Ok = false, Message = "Bạn chưa đăng nhập." });

        // tìm cart PENDING của user
        var cart = await _context.Orders
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "PENDING");

        if (cart == null)
            return NotFound(new ApiResult { Ok = false, Message = "Bạn chưa có giỏ hàng." });

        // tìm item trong cart
        var item = await _context.OrderItems
            .FirstOrDefaultAsync(oi => oi.OrderId == cart.OrderId && oi.AccountId == accountId);

        if (item == null)
            return NotFound(new ApiResult { Ok = false, Message = "Sản phẩm không tồn tại trong giỏ." });

        // xóa item
        _context.OrderItems.Remove(item);

        // cập nhật tổng tiền (an toàn nhất là tính lại từ DB)
        var newSubtotal = await _context.OrderItems
            .Where(oi => oi.OrderId == cart.OrderId && oi.AccountId != accountId)
            .SumAsync(oi => (long?)oi.UnitPrice) ?? 0;

        cart.TotalAmount = newSubtotal;

        await _context.SaveChangesAsync();

        return Ok(new ApiResult
        {
            Ok = true,
            Message = "Đã xóa sản phẩm khỏi giỏ hàng.",
            Data = new { cartId = cart.OrderId, subtotal = cart.TotalAmount }
        });
    }
    [HttpPost]
    [Route("api/cart/clear")]
    public async Task<IActionResult> ApiCartClear()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
            return Unauthorized(new ApiResult { Ok = false, Message = "Bạn chưa đăng nhập." });

        var cart = await _context.Orders
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "PENDING");

        if (cart == null)
            return Ok(new ApiResult { Ok = true, Message = "Giỏ hàng đang trống." });

        // xóa tất cả items trong cart
        var items = await _context.OrderItems
            .Where(oi => oi.OrderId == cart.OrderId)
            .ToListAsync();

        if (items.Count == 0)
            return Ok(new ApiResult { Ok = true, Message = "Giỏ hàng đang trống.", Data = new { cartId = cart.OrderId, subtotal = 0 } });

        _context.OrderItems.RemoveRange(items);

        cart.TotalAmount = 0;

        await _context.SaveChangesAsync();

        return Ok(new ApiResult
        {
            Ok = true,
            Message = "Đã xóa toàn bộ giỏ hàng.",
            Data = new { cartId = cart.OrderId, subtotal = 0 }
        });
    }

    public async Task<IActionResult> Checkout()
    {
        await Task.CompletedTask;
        return View();
    }
}

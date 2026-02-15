using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using SystemPurchaseAccGame.Dtos;
using SystemPurchaseAccGame.Models;
using SystemPurchaseAccGame.ViewModel;
using static SystemPurchaseAccGame.ViewModel.UserProfileDto;

public class OrderController : Controller
{
    private readonly GameAccShopContext _context;

    public OrderController(GameAccShopContext context)
    {
        _context = context;
    }

    private long? GetUserId()
    {
        if (User?.Identity?.IsAuthenticated != true) return null;
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(s, out var id) ? id : null;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreparePay(long id)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new AjaxModalResponse
            {
                Ok = false,
                Code = "NEED_LOGIN",
                Message = "Bạn cần đăng nhập để đặt hàng.",
                RedirectUrl = Url.Action("Login", "ClientHome", new { returnUrl = Request.Path + Request.QueryString })
            });
        }

        var acc = await _context.AccountListings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == id);

        if (acc == null)
            return BadRequest(new AjaxModalResponse { Ok = false, Code = "ERROR", Message = "Không tìm thấy account." });

        if (!string.Equals(acc.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new AjaxModalResponse { Ok = false, Code = "ERROR", Message = "Account hiện không khả dụng." });

        // Giá/Balance của bạn đang dùng long ở Wallet/Order/OrderItem => giữ long cho đồng bộ
        long price = acc.Price;

        var balance = await _context.Wallets.AsNoTracking()
            .Where(w => w.UserId == userId.Value)
            .Select(w => (long?)w.Balance)
            .FirstOrDefaultAsync() ?? 0;

        if (balance >= price)
        {
            var vm = new SystemPurchaseAccGame.ViewModel.ConfirmPayVm
            {
                AccountId = acc.AccountId,
                Title = acc.Title ?? $"Account #{acc.AccountId}",
                Price = price,
                Balance = balance
            };

            var html = await RenderPartialViewToStringAsync(
                "~/Views/Shared/Partials/New/_ConfirmPayModal.cshtml", vm);

            return Ok(new AjaxModalResponse { Ok = true, Code = "CONFIRM_PAY", Html = html });
        }

        var needMore = price - balance;
        var topupNote = $"NAP_{userId.Value}_{DateTime.UtcNow:yyyyMMddHHmmss}_{id}";

        // VietQR demo (đổi bank/stk/name thật của bạn)
        var bank = "VCB";
        var accNo = "0123456789";
        var accountName = "SYSTEM PURCHASE ACC GAME";
        var qrUrl =
            $"https://img.vietqr.io/image/{bank}-{accNo}-compact2.png" +
            $"?amount={needMore}" +
            $"&addInfo={Uri.EscapeDataString(topupNote)}" +
            $"&accountName={Uri.EscapeDataString(accountName)}";

        var vm2 = new SystemPurchaseAccGame.ViewModel.NeedTopupVm
        {
            AccountId = acc.AccountId,
            Title = acc.Title ?? $"Account #{acc.AccountId}",
            Price = price,
            Balance = balance,
            NeedMore = needMore,
            QrImageUrl = qrUrl,
            TopupNote = topupNote
        };

        var html2 = await RenderPartialViewToStringAsync(
            "~/Views/Shared/Partials/New/_NeedTopupModal.cshtml", vm2);

        return Ok(new AjaxModalResponse { Ok = true, Code = "NEED_TOPUP", Html = html2 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPay(long accountId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new AjaxModalResponse
            {
                Ok = false,
                Code = "NEED_LOGIN",
                Message = "Bạn cần đăng nhập."
            });
        }

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            // Lock Wallet (UPDLOCK)
            var wallet = await _context.Wallets
                .FromSqlInterpolated($@"SELECT * FROM Wallets WITH (UPDLOCK, ROWLOCK) WHERE UserId = {userId.Value}")
                .FirstOrDefaultAsync();

            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId.Value, Balance = 0, UpdatedAt = DateTime.UtcNow };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            // Lock AccountListing (UPDLOCK)
            var acc = await _context.AccountListings
                .FromSqlInterpolated($@"SELECT * FROM AccountListings WITH (UPDLOCK, ROWLOCK) WHERE AccountId = {accountId}")
                .FirstOrDefaultAsync();

            if (acc == null)
                return BadRequest(new AjaxModalResponse { Ok = false, Code = "ERROR", Message = "Không tìm thấy account." });

            if (!string.Equals(acc.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new AjaxModalResponse { Ok = false, Code = "ERROR", Message = "Account đã được bán hoặc không khả dụng." });

            long price = acc.Price;

            if (wallet.Balance < price)
            {
                await tx.RollbackAsync();
                return BadRequest(new AjaxModalResponse
                {
                    Ok = false,
                    Code = "NEED_TOPUP",
                    Message = "Số dư không đủ. Vui lòng nạp tiền."
                });
            }

            // Chặn mua trùng: OrderItem có Unique(AccountId) rồi, nhưng check trước vẫn tốt
            var existed = await _context.OrderItems.AsNoTracking().AnyAsync(x => x.AccountId == acc.AccountId);
            if (existed)
            {
                await tx.RollbackAsync();
                return BadRequest(new AjaxModalResponse { Ok = false, Code = "ERROR", Message = "Account này đã có đơn hàng." });
            }

            var now = DateTime.UtcNow;

            var order = new Order
            {
                UserId = userId.Value,
                TotalAmount = price,
                Status = "PAID",
                PaymentMethod = "WALLET",
                CreatedAt = now,
                PaidAt = now,
                Note = $"Thanh toán account {acc.AccountId}"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await RefreshBalanceClaimAsync(wallet.Balance);
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.OrderId,
                AccountId = acc.AccountId,
                UnitPrice = price
            });

            wallet.Balance -= price;
            wallet.UpdatedAt = now;

            acc.Status = "SOLD";

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            //return Ok(new AjaxModalResponse
            //{
            //    Ok = true,
            //    Code = "PAID_OK",
            //    Message = "Thanh toán thành công!",
            //    RedirectUrl = Url.Action("Success", "Order", new { id = order.OrderId })
            //});
            // Parse LoginInfo JSON từ acc.LoginInfo
            LoginInfoJson? li = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(acc.LoginInfo))
                    li = JsonSerializer.Deserialize<LoginInfoJson>(acc.LoginInfo);
            }
            catch
            {
                // nếu JSON lỗi thì li = null
            }

            var paidVm = new PaidAccountInfoVm
            {
                OrderId = order.OrderId,
                AccountId = acc.AccountId,
                Title = acc.Title ?? $"Account #{acc.AccountId}",
                Price = price,
                Username = li?.User ?? "(trống)",
                Password = li?.Pass ?? "(trống)",
                Note = li?.Note,
                MaskPublic = li?.MaskPublic ?? false,
                Hint = "Thông tin tài khoản được lưu ở lịch sử mua hàng."
            };

            var paidHtml = await RenderPartialViewToStringAsync(
                "~/Views/Shared/Partials/New/_PaidAccountInfoModal.cshtml",
                paidVm
            );

            return Ok(new AjaxModalResponse
            {
                Ok = true,
                Code = "PAID_OK",
                Message = "Thanh toán thành công!",
                Html = paidHtml,
                RedirectUrl = null // nếu muốn ở lại xem info
            });
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            return BadRequest(new AjaxModalResponse
            {
                Ok = false,
                Code = "ERROR",
                Message = "Lỗi database (có thể account vừa được mua)."
            });
        }
        catch
        {
            await tx.RollbackAsync();
            return BadRequest(new AjaxModalResponse { Ok = false, Code = "ERROR", Message = "Có lỗi khi thanh toán." });
        }
    }

    // ===== render partial -> string =====
    private async Task<string> RenderPartialViewToStringAsync(string viewName, object model)
    {
        ViewData.Model = model;

        await using var sw = new StringWriter();

        var engine = HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
        if (engine == null) throw new InvalidOperationException("Không lấy được ICompositeViewEngine.");

        var viewResult = engine.GetView(null, viewName, false);
        if (!viewResult.Success)
            viewResult = engine.FindView(ControllerContext, viewName, false);

        if (!viewResult.Success)
            throw new InvalidOperationException($"Không tìm thấy partial view: {viewName}");

        var viewContext = new ViewContext(
            ControllerContext,
            viewResult.View,
            ViewData,
            TempData,
            sw,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }
    private async Task RefreshBalanceClaimAsync(long newBalance)
    {
        // nếu chưa đăng nhập thì thôi
        if (User?.Identity?.IsAuthenticated != true) return;

        var identity = User.Identity as ClaimsIdentity;
        if (identity == null) return;

        // xóa claim balance cũ
        var old = identity.FindFirst("balance");
        if (old != null) identity.RemoveClaim(old);

        // add claim balance mới
        identity.AddClaim(new Claim("balance", newBalance.ToString(CultureInfo.InvariantCulture)));

        // phát hành lại cookie
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true // hoặc đọc từ cookie hiện tại nếu bạn muốn chuẩn hơn
            });
    }
}
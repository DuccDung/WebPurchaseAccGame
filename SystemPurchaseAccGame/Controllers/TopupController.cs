using Microsoft.AspNetCore.Mvc;

namespace SystemPurchaseAccGame.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;
    using SystemPurchaseAccGame.Models;
    using System.Text.Json;
    [Route("client/topup")]
    public class TopupController : Controller
    {
        private readonly GameAccShopContext _context;
        public TopupController(GameAccShopContext context)
        {
            _context = context;
        }
        [HttpPost("card")]
        public async Task<IActionResult> Card([FromForm] CardTopupRequest req)
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
                        var rawPayload = JsonSerializer.Serialize(new
                        {
                            pin = req.pin,
                            serial = req.serial
                        });
                        var topup = new Topup
                        {
                            UserId = u.UserId,
                            Amount = req.amount,
                            Method = "Card",
                            Status = "PENDING",
                            CreatedAt = DateTime.UtcNow,
                            RawPayload = rawPayload,
                            Provider = req.provider
                        };
                        await _context.Topups.AddAsync(topup);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            return RedirectToAction("PaymentSuccess", "Home");
        }

        [HttpPost("bank")]
        public async Task<IActionResult> Bank([FromForm] BankTopupRequest req)
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
                        var topup = new Topup
                        {
                            UserId = u.UserId,
                            Amount = req.amount,
                            Method = "BANK",
                            Status = "PENDING",
                            CreatedAt = DateTime.UtcNow,
                            ReferenceCode = req.txn_code,
                        };
                        await _context.Topups.AddAsync(topup);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            return RedirectToAction("PaymentSuccess" , "Home");
        }
    }

    public class CardTopupRequest
    {
        public string provider { get; set; } = "";
        public int amount { get; set; }
        public string pin { get; set; } = "";
        public string serial { get; set; } = "";
    }

    public class BankTopupRequest
    {
        public int amount { get; set; }
        public string txn_code { get; set; } = "";
        public string bank_name { get; set; } = "";
        public string bank_account { get; set; } = "";
        public string bank_owner { get; set; } = "";
        public string transfer_prefix { get; set; } = "";
        public string transfer_content { get; set; } = "";
    }
}

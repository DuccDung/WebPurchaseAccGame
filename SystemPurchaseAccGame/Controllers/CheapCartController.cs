using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SystemPurchaseAccGame.Dtos;
using SystemPurchaseAccGame.Models;

namespace SystemPurchaseAccGame.Controllers
{
    [ApiController]
    public class CheapCartController : ControllerBase
    {
        private readonly GameAccShopContext _context;

        public CheapCartController(GameAccShopContext context)
        {
            _context = context;
        }

        public class AddCheapToCartReq
        {
            public int GameId { get; set; }
            public int Quantity { get; set; }
        }

        // POST: /api/cart/add-cheap
        [HttpPost]
        [Route("api/cart/add-cheap")]
        public async Task<IActionResult> AddCheap([FromBody] AddCheapToCartReq req)
        {
            // 1) Auth
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out var userId))
                return Unauthorized(new ApiResult { Ok = false, Message = "Bạn chưa đăng nhập." });

            if (req == null || req.GameId <= 0)
                return BadRequest(new ApiResult { Ok = false, Message = "GameId không hợp lệ." });

            var qty = req.Quantity;
            if (qty < 1) qty = 1;
            if (qty > 99) qty = 99;

            // 2) chỉ cho game thuộc cateId = 6
            var game = await _context.Games
                .AsNoTracking()
                .Where(g => g.GameId == req.GameId && g.CategoryId == 6)
                .Select(g => new { g.GameId, g.Name })
                .FirstOrDefaultAsync();

            if (game == null)
                return BadRequest(new ApiResult
                {
                    Ok = false,
                    Message = "Game không tồn tại hoặc không thuộc danh mục giá rẻ (cate=6)."
                });

            // 3) Transaction tránh race condition
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 4) lấy / tạo cart PENDING
                var cart = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "PENDING");

                if (cart == null)
                {
                    cart = new Order
                    {
                        UserId = userId,
                        Status = "PENDING",
                        PaymentMethod = "WALLET",
                        TotalAmount = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Orders.Add(cart);
                    await _context.SaveChangesAsync(); // có OrderId
                }

                // 5) chọn N account AVAILABLE, chưa nằm trong OrderItems (để không dính unique)
                // “giống nhau” => lấy đại theo CreatedAt
                var candidates = await _context.AccountListings
                    .AsNoTracking()
                    .Where(a => a.GameId == req.GameId && a.Status == "AVAILABLE")
                    .Where(a => !_context.OrderItems.Any(oi => oi.AccountId == a.AccountId))
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => new { a.AccountId, a.Price })
                    .Take(qty)
                    .ToListAsync();

                if (candidates.Count < qty)
                {
                    await tx.RollbackAsync();
                    return Conflict(new ApiResult
                    {
                        Ok = false,
                        Message = $"Không đủ hàng. Hiện chỉ còn {candidates.Count} acc có thể mua."
                    });
                }

                long addedTotal = 0;

                foreach (var acc in candidates)
                {
                    // tránh add trùng trong cart (an toàn)
                    var existsInCart = await _context.OrderItems
                        .AnyAsync(oi => oi.OrderId == cart.OrderId && oi.AccountId == acc.AccountId);

                    if (existsInCart) continue;

                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = cart.OrderId,
                        AccountId = acc.AccountId,
                        UnitPrice = acc.Price
                    });

                    addedTotal += acc.Price;
                }

                cart.TotalAmount += addedTotal;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new ApiResult
                {
                    Ok = true,
                    Message = $"Đã thêm {qty} acc vào giỏ hàng.",
                    Data = new { cartId = cart.OrderId, added = qty, total = cart.TotalAmount }
                });
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                return Conflict(new ApiResult
                {
                    Ok = false,
                    Message = "Một số acc vừa được người khác thêm/mua. Vui lòng thử lại."
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new ApiResult
                {
                    Ok = false,
                    Message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }
    }
}
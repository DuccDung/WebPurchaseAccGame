using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SystemPurchaseAccGame.Dtos;
using SystemPurchaseAccGame.Models;

namespace SystemPurchaseAccGame.Controllers
{
    [ApiController]
    [Route("api/cheap-account-listings")]
    public class CheapAccountListingsController : ControllerBase
    {
        private readonly GameAccShopContext _context;
        private readonly IWebHostEnvironment _env;

        private const int CHEAP_CATEGORY_ID = 6;

        public CheapAccountListingsController(GameAccShopContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] AccountListingCreateRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // ✅ chỉ cho game category 6
            var game = await _context.Games
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GameId == request.GameId && g.CategoryId == CHEAP_CATEGORY_ID);

            if (game == null)
                return BadRequest(new { message = "GameId không tồn tại hoặc không thuộc category giá rẻ." });

            // Parse AttributesJson
            List<AttributeItemDto> attributes = new();
            if (!string.IsNullOrWhiteSpace(request.AttributesJson))
            {
                try
                {
                    attributes = JsonSerializer.Deserialize<List<AttributeItemDto>>(
                        request.AttributesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new();
                }
                catch
                {
                    return BadRequest(new { message = "AttributesJson invalid JSON." });
                }
            }

            attributes = attributes
                .Where(a => !string.IsNullOrWhiteSpace(a.Key))
                .Select(a => new AttributeItemDto
                {
                    Key = a.Key.Trim(),
                    Value = (a.Value ?? "").Trim()
                })
                .ToList();

            var loginInfoJson = request.LoginInfo == null ? null : JsonSerializer.Serialize(request.LoginInfo);

            var now = DateTime.UtcNow;

            // ✅ price tự lấy từ game.Price
            var gamePriceDecimal = game.Price ?? 0m;
            var listingPriceLong = (long)Math.Round(gamePriceDecimal, 0, MidpointRounding.AwayFromZero);

            var listing = new AccountListing
            {
                GameId = request.GameId,
                Title = request.Title.Trim(),
                Price = listingPriceLong,              // ✅ AUTO
                Description = request.Description,
                LoginInfo = loginInfoJson,
                Status = request.Status,
                CreatedAt = now,
                UpdatedAt = null
            };

            _context.AccountListings.Add(listing);
            await _context.SaveChangesAsync();

            // Save files
            var mediaEntities = new List<AccountMedium>();
            var imgFolder = Path.Combine(_env.WebRootPath, "img");
            if (!Directory.Exists(imgFolder))
                Directory.CreateDirectory(imgFolder);

            async Task<string> SaveToImgFolderAsync(IFormFile file)
            {
                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".bin";

                var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(imgFolder, fileName);

                await using var stream = System.IO.File.Create(fullPath);
                await file.CopyToAsync(stream);

                return $"/img/{fileName}";
            }

            if (request.BackgroundFile != null && request.BackgroundFile.Length > 0)
            {
                var url = await SaveToImgFolderAsync(request.BackgroundFile);
                mediaEntities.Add(new AccountMedium
                {
                    AccountId = listing.AccountId,
                    MediaType = "background",
                    Url = url,
                    SortOrder = 0,
                    CreatedAt = now
                });
            }

            if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
            {
                var url = await SaveToImgFolderAsync(request.ThumbnailFile);
                mediaEntities.Add(new AccountMedium
                {
                    AccountId = listing.AccountId,
                    MediaType = "thumbnail",
                    Url = url,
                    SortOrder = 0,
                    CreatedAt = now
                });
            }

            if (request.GalleryFiles != null && request.GalleryFiles.Count > 0)
            {
                var order = 1;
                foreach (var f in request.GalleryFiles.Where(x => x != null && x.Length > 0))
                {
                    var url = await SaveToImgFolderAsync(f);
                    mediaEntities.Add(new AccountMedium
                    {
                        AccountId = listing.AccountId,
                        MediaType = "gallery",
                        Url = url,
                        SortOrder = order++,
                        CreatedAt = now
                    });
                }
            }

            if (mediaEntities.Count > 0)
                _context.AccountMedia.AddRange(mediaEntities);

            if (attributes.Count > 0)
            {
                var attrEntities = attributes.Select(a => new AccountAttribute
                {
                    AccountId = listing.AccountId,
                    AttrKey = a.Key,
                    AttrValue = a.Value ?? "",
                    CreatedAt = now
                }).ToList();

                _context.AccountAttributes.AddRange(attrEntities);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                accountId = listing.AccountId,
                message = "Tạo bài đăng acc giá rẻ thành công.",
                gamePrice = gamePriceDecimal,
                listingPrice = listingPriceLong
            });
        }
    }
}
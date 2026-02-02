using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SystemPurchaseAccGame.Dtos;
using SystemPurchaseAccGame.Models;

namespace SystemPurchaseAccGame.Controllers
{
    [ApiController]
    [Route("api/account-listings")]
    public class AccountListingsController : ControllerBase
    {
        private readonly GameAccShopContext _context;
        private readonly IWebHostEnvironment _env;

        public AccountListingsController(GameAccShopContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] AccountListingCreateRequest request)
        {
            // 1) Validate (ApiController tự trả 400 nếu invalid, đoạn này để rõ ràng)
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // 2) Check game tồn tại
            var gameExists = await _context.Games.AnyAsync(g => g.GameId == request.GameId);
            if (!gameExists)
                return BadRequest(new { message = "GameId không tồn tại." });

            // 3) Parse AttributesJson
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

            // 4) Chuẩn hoá attrs (tránh null/empty key)
            attributes = attributes
                .Where(a => !string.IsNullOrWhiteSpace(a.Key))
                .Select(a => new AttributeItemDto
                {
                    Key = a.Key.Trim(),
                    Value = (a.Value ?? "").Trim()
                })
                .ToList();

            // 5) LoginInfo -> JSON string (lưu vào AccountListing.LoginInfo)
            var loginInfoJson = request.LoginInfo == null
                ? null
                : JsonSerializer.Serialize(request.LoginInfo);

            // 6) Tạo AccountListing trước để lấy AccountId
            var now = DateTime.UtcNow;

            var listing = new AccountListing
            {
                GameId = request.GameId,
                Title = request.Title.Trim(),
                Price = Convert.ToInt64(request.Price), // DB của bạn là long
                Description = request.Description,
                LoginInfo = loginInfoJson,
                Status = request.Status,
                CreatedAt = now,
                UpdatedAt = null
            };

            _context.AccountListings.Add(listing);
            await _context.SaveChangesAsync(); // có AccountId

            // 7) Save files to wwwroot/img và tạo AccountMedia
            var mediaEntities = new List<AccountMedium>();

            // wwwroot/img
            var imgFolder = Path.Combine(_env.WebRootPath, "img");
            if (!Directory.Exists(imgFolder))
                Directory.CreateDirectory(imgFolder);

            async Task<string> SaveToImgFolderAsync(IFormFile file)
            {
                // tên file không trùng
                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".bin";

                // Option: có thể whitelist ext nếu muốn (jpg/png/webp)
                var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(imgFolder, fileName);

                await using var stream = System.IO.File.Create(fullPath);
                await file.CopyToAsync(stream);

                // Url lưu DB: /img/xxx.jpg
                return $"/img/{fileName}";
            }

            // Background
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

            // Thumbnail
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

            // Gallery (SortOrder tăng dần)
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
            {
                _context.AccountMedia.AddRange(mediaEntities);
            }

            // 8) Add AccountAttributes
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

            // 9) Save all
            await _context.SaveChangesAsync();

            // 10) Return
            return Ok(new
            {
                success = true,
                accountId = listing.AccountId,
                message = "Tạo bài đăng thành công.",
                mediaCount = mediaEntities.Count,
                attrCount = attributes.Count
            });
        }
    }
}

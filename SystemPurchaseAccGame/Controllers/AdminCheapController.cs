using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using SystemPurchaseAccGame.Models;
using SystemPurchaseAccGame.ViewModel;

namespace SystemPurchaseAccGame.Controllers
{
    public class AdminCheapController : Controller
    {
        private readonly GameAccShopContext _context;

        // cố định categoryId = 6
        private const int CHEAP_CATEGORY_ID = 6;

        public AdminCheapController(GameAccShopContext context)
        {
            _context = context;
        }

        // =========================
        // Helpers
        // =========================
        private static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            input = input.Trim().ToLowerInvariant();

            // replace Vietnamese accents (simple)
            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                sb.Append(c switch
                {
                    'à' or 'á' or 'ạ' or 'ả' or 'ã' or 'â' or 'ầ' or 'ấ' or 'ậ' or 'ẩ' or 'ẫ' or 'ă' or 'ằ' or 'ắ' or 'ặ' or 'ẳ' or 'ẵ' => 'a',
                    'è' or 'é' or 'ẹ' or 'ẻ' or 'ẽ' or 'ê' or 'ề' or 'ế' or 'ệ' or 'ể' or 'ễ' => 'e',
                    'ì' or 'í' or 'ị' or 'ỉ' or 'ĩ' => 'i',
                    'ò' or 'ó' or 'ọ' or 'ỏ' or 'õ' or 'ô' or 'ồ' or 'ố' or 'ộ' or 'ổ' or 'ỗ' or 'ơ' or 'ờ' or 'ớ' or 'ợ' or 'ở' or 'ỡ' => 'o',
                    'ù' or 'ú' or 'ụ' or 'ủ' or 'ũ' or 'ư' or 'ừ' or 'ứ' or 'ự' or 'ử' or 'ữ' => 'u',
                    'ỳ' or 'ý' or 'ỵ' or 'ỷ' or 'ỹ' => 'y',
                    'đ' => 'd',
                    _ => c
                });
            }

            // keep a-z0-9 and spaces -> dash
            var cleaned = new StringBuilder();
            bool dash = false;
            foreach (var c in sb.ToString())
            {
                if (char.IsLetterOrDigit(c))
                {
                    cleaned.Append(c);
                    dash = false;
                }
                else
                {
                    if (!dash)
                    {
                        cleaned.Append('-');
                        dash = true;
                    }
                }
            }

            return cleaned.ToString().Trim('-');
        }

        private async Task<bool> SlugExistsAsync(string slug, int? excludeGameId = null)
        {
            slug = (slug ?? "").Trim();
            if (string.IsNullOrWhiteSpace(slug)) return false;

            return await _context.Games.AsNoTracking().AnyAsync(g =>
                g.Slug == slug && (!excludeGameId.HasValue || g.GameId != excludeGameId.Value));
        }

        // =========================
        // PARTIAL VIEW: Quản lý game giá rẻ
        // =========================
        [HttpGet]
        public async Task<IActionResult> CheapGames()
        {
            var list = await _context.Games
                .AsNoTracking()
                .Where(g => g.CategoryId == CHEAP_CATEGORY_ID)
                .OrderByDescending(g => g.GameId)
                .Select(g => new CheapGameRowVm
                {
                    GameId = g.GameId,
                    CategoryId = g.CategoryId,
                    Name = g.Name,
                    Slug = g.Slug,
                    ThumbnailUrl = g.ThumbnailUrl,
                    IsActive = g.IsActive,
                    Price = g.Price,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync();

            return PartialView("Partials/AdminCheap/_CheapGames", list);
        }

        // =========================
        // PARTIAL VIEW: Tạo acc giá rẻ
        // (chỉ load game category 6)
        // =========================
        [HttpGet]
        public async Task<IActionResult> CheapAccountListing()
        {
            var games = await _context.Games
                .AsNoTracking()
                .Where(g => g.CategoryId == CHEAP_CATEGORY_ID && g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();

            ViewBag.Games = games;
            return PartialView("Partials/AdminCheap/_CheapAccountListing");
        }

        // =========================
        // API: CRUD game giá rẻ
        // =========================
        [HttpPost]
        [Route("/admin/cheap/api/game/create")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiCreateCheapGame([FromBody] CheapGameCreateDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { success = false, message = "Tên game không hợp lệ." });

            var name = req.Name.Trim();
            var slug = string.IsNullOrWhiteSpace(req.Slug) ? Slugify(name) : Slugify(req.Slug);

            if (string.IsNullOrWhiteSpace(slug))
                return BadRequest(new { success = false, message = "Slug không hợp lệ." });

            if (await SlugExistsAsync(slug))
                return BadRequest(new { success = false, message = "Slug đã tồn tại." });

            var game = new Game
            {
                CategoryId = CHEAP_CATEGORY_ID, // ✅ cố định
                Name = name,
                Slug = slug,
                ThumbnailUrl = string.IsNullOrWhiteSpace(req.ThumbnailUrl) ? null : req.ThumbnailUrl.Trim(),
                IsActive = req.IsActive,
                Price = req.Price ?? 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã tạo game giá rẻ.",
                data = new
                {
                    gameId = game.GameId,
                    categoryId = game.CategoryId,
                    name = game.Name,
                    slug = game.Slug,
                    thumbnailUrl = game.ThumbnailUrl,
                    isActive = game.IsActive,
                    price = game.Price,
                    createdAt = game.CreatedAt
                }
            });
        }

        [HttpPost]
        [Route("/admin/cheap/api/game/update")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiUpdateCheapGame([FromBody] CheapGameUpdateDto req)
        {
            if (req == null || req.GameId <= 0)
                return BadRequest(new { success = false, message = "GameId không hợp lệ." });

            var game = await _context.Games.FirstOrDefaultAsync(g => g.GameId == req.GameId);
            if (game == null)
                return NotFound(new { success = false, message = "Không tìm thấy game." });

            if (game.CategoryId != CHEAP_CATEGORY_ID)
                return BadRequest(new { success = false, message = "Game này không thuộc category giá rẻ (6)." });

            var name = (req.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { success = false, message = "Tên game không hợp lệ." });

            var slug = string.IsNullOrWhiteSpace(req.Slug) ? Slugify(name) : Slugify(req.Slug);

            if (await SlugExistsAsync(slug, excludeGameId: req.GameId))
                return BadRequest(new { success = false, message = "Slug đã tồn tại." });

            game.Name = name;
            game.Slug = slug;
            game.ThumbnailUrl = string.IsNullOrWhiteSpace(req.ThumbnailUrl) ? null : req.ThumbnailUrl.Trim();
            game.IsActive = req.IsActive;
            game.Price = req.Price ?? 0;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã cập nhật game giá rẻ.",
                data = new
                {
                    gameId = game.GameId,
                    name = game.Name,
                    slug = game.Slug,
                    thumbnailUrl = game.ThumbnailUrl,
                    isActive = game.IsActive,
                    price = game.Price
                }
            });
        }

        [HttpPost]
        [Route("/admin/cheap/api/game/delete")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiDeleteCheapGame([FromBody] CheapGameDeleteDto req)
        {
            if (req == null || req.GameId <= 0)
                return BadRequest(new { success = false, message = "GameId không hợp lệ." });

            var game = await _context.Games.FirstOrDefaultAsync(g => g.GameId == req.GameId);
            if (game == null)
                return NotFound(new { success = false, message = "Không tìm thấy game." });

            if (game.CategoryId != CHEAP_CATEGORY_ID)
                return BadRequest(new { success = false, message = "Game này không thuộc category giá rẻ (6)." });

            // chặn xóa nếu đang có listing
            var hasListing = await _context.AccountListings.AsNoTracking().AnyAsync(a => a.GameId == req.GameId);
            if (hasListing)
                return BadRequest(new { success = false, message = "Game đang có bài đăng, không thể xóa." });

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa game giá rẻ." });
        }
    }
}
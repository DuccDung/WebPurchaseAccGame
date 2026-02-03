using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemPurchaseAccGame.Models;
using SystemPurchaseAccGame.ViewModel;

public class HomeController : Controller
{
    private readonly GameAccShopContext _context;

    public HomeController(GameAccShopContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> HomePage()
    {
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

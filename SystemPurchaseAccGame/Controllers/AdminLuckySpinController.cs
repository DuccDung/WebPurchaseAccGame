using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemPurchaseAccGame.Models;

public class AdminLuckySpinController : Controller
{
    private readonly GameAccShopContext _context;
    public AdminLuckySpinController(GameAccShopContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _context.LuckySpinItems
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.ItemId)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(new LuckySpinItem());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LuckySpinItem m)
    {
        if (!ModelState.IsValid) return View(m);
        m.CreatedAt = DateTime.UtcNow;
        _context.LuckySpinItems.Add(m);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var m = await _context.LuckySpinItems.FindAsync(id);
        if (m == null) return NotFound();
        return View(m);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, LuckySpinItem m)
    {
        if (id != m.ItemId) return BadRequest();
        if (!ModelState.IsValid) return View(m);

        var db = await _context.LuckySpinItems.FirstOrDefaultAsync(x => x.ItemId == id);
        if (db == null) return NotFound();

        db.Title = m.Title;
        db.PrizeTier = m.PrizeTier;
        db.PrizeValue = m.PrizeValue;
        db.AccountUser = m.AccountUser;
        db.AccountPass = m.AccountPass;
        db.Weight = m.Weight;
        db.Remaining = m.Remaining;
        db.IsActive = m.IsActive;
        db.WinMessage = m.WinMessage;
        db.Note = m.Note;
        db.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var db = await _context.LuckySpinItems.FindAsync(id);
        if (db != null)
        {
            _context.LuckySpinItems.Remove(db);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
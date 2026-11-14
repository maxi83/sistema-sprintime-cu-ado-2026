using lucho_sprintime.Services;
using lucho_sprintime.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class RankingController(IRankingService rankingService, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var settings = await db.SocialLinks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1);
        if (settings?.RankingEnabled != true) return NotFound();
        return View(await rankingService.BuildAsync());
    }
}

using lucho_sprintime.Data;
using lucho_sprintime.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class FollowController(ApplicationDbContext db) : Controller
{
    [HttpGet("seguir/{slug}")]
    public async Task<IActionResult> Index(string slug, string? q = null)
    {
        var race = await db.Events.AsNoTracking().Include(x => x.Checkpoints).FirstOrDefaultAsync(x => x.Slug == slug);
        if (race is null) return NotFound();
        ViewBag.Event = race; ViewBag.Query = q;
        if (string.IsNullOrWhiteSpace(q)) return View(new List<Registration>());
        q = q.Trim();
        var matches = await db.Registrations.AsNoTracking()
            .Where(x => x.EventId == race.Id && x.PaymentStatus == PaymentStatus.Approved)
            .Include(x => x.Cyclist).Include(x => x.Category).Include(x => x.EventDistance)
            .Include(x => x.TimingRecords).ThenInclude(x => x.Checkpoint)
            .Where(x => (x.BibNumber ?? "") == q || x.Cyclist.FirstName.Contains(q) || x.Cyclist.LastName.Contains(q))
            .Take(8).ToListAsync();
        return View(matches);
    }
}

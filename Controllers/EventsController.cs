using lucho_sprintime.Data;
using lucho_sprintime.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class EventsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? state = null)
    {
        var query = db.Events.AsNoTracking().Include(x => x.Distances).Where(x => x.Status != EventStatus.Draft &&
            x.Slug != "desafio-mtb-potrero-2026" && x.Slug != "rally-bike-la-florida-2026");
        query = state?.ToLowerInvariant() switch
        {
            "past" or "historial" => query.Where(x => x.StartDate < DateTime.Now || x.Status == EventStatus.Finished),
            _ => query.Where(x => x.StartDate >= DateTime.Now && x.Status != EventStatus.Finished)
        };
        ViewBag.ShowHistory = state is "past" or "historial";
        return View(await (state is "past" or "historial" ? query.OrderByDescending(x => x.StartDate) : query.OrderBy(x => x.StartDate)).ToListAsync());
    }

    [HttpGet("carreras/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var race = await db.Events.AsNoTracking()
            .Include(x => x.Distances).ThenInclude(x => x.Categories)
            .Include(x => x.HistoricPodiums)
            .Include(x => x.Sponsors)
            .Include(x => x.Checkpoints)
            .Include(x => x.Registrations.Where(r => r.Status == RegistrationStatus.Confirmed)).ThenInclude(x => x.Cyclist)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.Status != EventStatus.Draft && x.Slug != "desafio-mtb-potrero-2026" && x.Slug != "rally-bike-la-florida-2026");
        return race is null ? NotFound() : View(race);
    }
}

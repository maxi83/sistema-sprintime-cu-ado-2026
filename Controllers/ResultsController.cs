using lucho_sprintime.Data;
using lucho_sprintime.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class ResultsController(ApplicationDbContext db, lucho_sprintime.Services.IExportService exportService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Latest()
    {
        var race = await db.Events.AsNoTracking()
            .Where(x => x.Slug != "desafio-mtb-potrero-2026" && x.Slug != "rally-bike-la-florida-2026")
            .Where(x => x.Status == EventStatus.Finished && db.Results.Any(r =>
                r.Registration.EventId == x.Id && r.Status == ResultStatus.Official))
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();
        return race is null ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Event), new { slug = race.Slug });
    }

    public async Task<IActionResult> Index()
        => View(await db.Events.AsNoTracking().Where(x => x.Status == EventStatus.Finished || db.Results.Any(r => r.Registration.EventId == x.Id)).OrderByDescending(x => x.StartDate).ToListAsync());

    [HttpGet("resultados/{slug}")]
    public async Task<IActionResult> Event(string slug, string? q = null, int? categoryId = null)
    {
        var race = await db.Events.AsNoTracking().Include(x => x.Distances).ThenInclude(x => x.Categories).FirstOrDefaultAsync(x => x.Slug == slug);
        if (race is null) return NotFound();
        IQueryable<Result> query = db.Results.AsNoTracking()
            .Where(x => x.Registration.EventId == race.Id)
            .Include(x => x.Registration).ThenInclude(x => x.Cyclist)
            .Include(x => x.Registration).ThenInclude(x => x.Category)
            .Include(x => x.Registration).ThenInclude(x => x.EventDistance);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Registration.Cyclist.FirstName.Contains(q) || x.Registration.Cyclist.LastName.Contains(q) || (x.Registration.BibNumber ?? "").Contains(q));
        if (categoryId.HasValue) query = query.Where(x => x.Registration.CategoryId == categoryId.Value);
        ViewBag.Event = race; ViewBag.Query = q; ViewBag.CategoryId = categoryId;
        return View(await query.OrderBy(x => x.GeneralPosition).ToListAsync());
    }

    [Authorize(Roles = "Administrator,Organizer"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int eventId)
    {
        var results = await db.Results.Include(x => x.Registration).Where(x => x.Registration.EventId == eventId).ToListAsync();
        foreach (var r in results) { r.Status = ResultStatus.Official; r.PublishedAt = DateTime.UtcNow; }
        var race = await db.Events.FirstOrDefaultAsync(x => x.Id == eventId); if (race is not null) race.Status = EventStatus.Finished;
        await db.SaveChangesAsync(); TempData["Success"] = "Resultados publicados como oficiales.";
        return race is null ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Event), new { slug = race.Slug });
    }
    [HttpGet("finisher/{id:int}")]
    public async Task<IActionResult> Finisher(int id)
    {
        var result = await db.Results.AsNoTracking()
            .Include(x => x.Registration).ThenInclude(x => x.Event)
            .Include(x => x.Registration).ThenInclude(x => x.Cyclist)
            .Include(x => x.Registration).ThenInclude(x => x.Category)
            .Include(x => x.Registration).ThenInclude(x => x.EventDistance)
            .FirstOrDefaultAsync(x => x.Id == id);
        return result is null ? NotFound() : View(result);
    }

    [HttpGet("diploma/{id:int}.pdf")]
    public async Task<IActionResult> Diploma(int id)
    {
        var result = await db.Results.AsNoTracking()
            .Include(x => x.Registration).ThenInclude(x => x.Event)
            .Include(x => x.Registration).ThenInclude(x => x.Cyclist)
            .Include(x => x.Registration).ThenInclude(x => x.Category)
            .Include(x => x.Registration).ThenInclude(x => x.EventDistance)
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == ResultStatus.Official);
        if (result is null) return NotFound();
        return File(exportService.BuildDiploma(result), "application/pdf", $"diploma-{result.Registration.Event.Slug}-{result.Registration.BibNumber ?? result.Id.ToString()}.pdf");
    }

}

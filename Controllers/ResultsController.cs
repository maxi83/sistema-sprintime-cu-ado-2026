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
            .Where(x => (x.Status == EventStatus.Finished || x.StartDate < DateTime.Now)
                && (db.HistoricPodiums.Any(p => p.EventId == x.Id) ||
                    db.Results.Any(r => r.Registration.EventId == x.Id && r.Status == ResultStatus.Official) || db.ManualPodiums.Any(p => p.EventId == x.Id && p.IsPublished)))
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();
        if (race is null) return RedirectToAction(nameof(Index));
        var hasOfficialResults = await db.Results.AnyAsync(r =>
            r.Registration.EventId == race.Id && r.Status == ResultStatus.Official) || await db.ManualPodiums.AnyAsync(p => p.EventId == race.Id && p.IsPublished);
        return hasOfficialResults
            ? RedirectToAction(nameof(Event), new { slug = race.Slug })
            : RedirectToAction("Details", "Events", new { slug = race.Slug });
    }

    [HttpGet("resultados")]
    public async Task<IActionResult> Index()
        => View(await db.Events.AsNoTracking().Where(x => x.Slug != "desafio-mtb-potrero-2026" && x.Slug != "rally-bike-la-florida-2026")
            .Where(x => x.Status == EventStatus.Finished && (db.Results.Any(r => r.Registration.EventId == x.Id && r.Status == ResultStatus.Official) || db.ManualPodiums.Any(p => p.EventId == x.Id && p.IsPublished))).OrderByDescending(x => x.StartDate).ToListAsync());

    [HttpGet("resultados/{slug}")]
    public async Task<IActionResult> Event(string slug, string? q = null, int? categoryId = null)
    {
        var race = await db.Events.AsNoTracking().Include(x => x.Distances).ThenInclude(x => x.Categories).FirstOrDefaultAsync(x => x.Slug == slug);
        if (race is null) return NotFound();
        // Los organizadores pueden revisar resultados provisionales sin publicarlos.
        // Para el público solo son visibles los resultados oficiales.
        var organizerPreview = User.IsInRole("Administrator") || User.IsInRole("Organizer");
        IQueryable<Result> query = db.Results.AsNoTracking()
            .Where(x => x.Registration.EventId == race.Id && (x.Status == ResultStatus.Official || organizerPreview))
            .Include(x => x.Registration).ThenInclude(x => x.Cyclist)
            .Include(x => x.Registration).ThenInclude(x => x.Category)
            .Include(x => x.Registration).ThenInclude(x => x.EventDistance);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Registration.Cyclist.FirstName.Contains(q) || x.Registration.Cyclist.LastName.Contains(q) || (x.Registration.BibNumber ?? "").Contains(q));
        if (categoryId.HasValue) query = query.Where(x => x.Registration.CategoryId == categoryId.Value);
        ViewBag.Event = race; ViewBag.Query = q; ViewBag.CategoryId = categoryId;
        ViewBag.OrganizerPreview = organizerPreview;
        ViewBag.ManualPodiums = await db.ManualPodiums.AsNoTracking()
            .Where(x => x.EventId == race.Id && (x.IsPublished || organizerPreview))
            .Include(x => x.Registration).ThenInclude(x => x.Cyclist)
            .Include(x => x.Category)
            .OrderBy(x => x.Category.Name).ThenBy(x => x.Place).ToListAsync();
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
    [Authorize(Roles = "Administrator,Organizer"), HttpGet]
    public async Task<IActionResult> ManualPodiums(int eventId)
    {
        var race = await db.Events.AsNoTracking().Include(x => x.Distances)
            .ThenInclude(x => x.Categories).FirstOrDefaultAsync(x => x.Id == eventId);
        if (race is null) return NotFound();
        var entries = await db.ManualPodiums.AsNoTracking()
            .Where(x => x.EventId == eventId)
            .Include(x => x.Registration).ThenInclude(x => x.Cyclist)
            .Include(x => x.Category)
            .OrderBy(x => x.Category.Name).ThenBy(x => x.Place).ToListAsync();
        ViewBag.Event = race;
        return View(entries);
    }

    [Authorize(Roles = "Administrator,Organizer"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveManualPodium(int eventId, int categoryId, int place, string? bibNumber)
    {
        var race = await db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.Id == eventId);
        if (race is null) return NotFound();
        if (place < 1 || place > 5 || string.IsNullOrWhiteSpace(bibNumber))
        {
            TempData["Error"] = "Ingresá un dorsal válido y un puesto del 1.º al 5.º.";
            return RedirectToAction(nameof(ManualPodiums), new { eventId });
        }
        var category = await db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == categoryId && x.EventDistance.EventId == eventId);
        if (category is null) return NotFound();
        var bib = bibNumber.Trim();
        var registration = await db.Registrations.AsNoTracking()
            .Where(x => x.EventId == eventId && x.CategoryId == categoryId && x.BibNumber == bib)
            .FirstOrDefaultAsync();
        if (registration is null)
        {
            TempData["Error"] = "Ese dorsal no está inscrito en la categoría seleccionada. Revisá su categoría en Participantes.";
            return RedirectToAction(nameof(ManualPodiums), new { eventId });
        }
        if (registration.PaymentStatus != PaymentStatus.Approved || registration.Status is RegistrationStatus.Cancelled or RegistrationStatus.Refunded)
        {
            TempData["Error"] = "El ciclista debe tener su inscripción confirmada y el pago aprobado.";
            return RedirectToAction(nameof(ManualPodiums), new { eventId });
        }
        var entries = await db.ManualPodiums.Where(x => x.EventId == eventId).ToListAsync();
        if (entries.Any(x => x.IsPublished))
        {
            TempData["Error"] = "Los podios están publicados. No se pueden modificar sin un procedimiento de rectificación.";
            return RedirectToAction(nameof(ManualPodiums), new { eventId });
        }
        var occupied = entries.FirstOrDefault(x => x.CategoryId == categoryId && x.Place == place);
        var repeated = entries.FirstOrDefault(x => x.RegistrationId == registration.Id);
        if (repeated is not null && repeated != occupied)
        {
            TempData["Error"] = "Ese dorsal ya tiene un puesto asignado en esta carrera.";
            return RedirectToAction(nameof(ManualPodiums), new { eventId });
        }
        if (occupied is null)
        {
            db.ManualPodiums.Add(new ManualPodium { EventId = eventId, CategoryId = categoryId,
                RegistrationId = registration.Id, Place = place });
        }
        else occupied.RegistrationId = registration.Id;
        await db.SaveChangesAsync();
        TempData["Success"] = "Puesto guardado como borrador. Todavía no es público.";
        return RedirectToAction(nameof(ManualPodiums), new { eventId });
    }

    [Authorize(Roles = "Administrator,Organizer"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteManualPodium(int eventId, int id)
    {
        var entry = await db.ManualPodiums.FirstOrDefaultAsync(x => x.Id == id && x.EventId == eventId);
        if (entry is null) return NotFound();
        if (entry.IsPublished)
            TempData["Error"] = "Un puesto publicado no puede eliminarse sin una rectificación.";
        else
        {
            db.ManualPodiums.Remove(entry);
            await db.SaveChangesAsync();
            TempData["Success"] = "Puesto eliminado del borrador.";
        }
        return RedirectToAction(nameof(ManualPodiums), new { eventId });
    }

    [Authorize(Roles = "Administrator,Organizer"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishManualPodiums(int eventId)
    {
        var race = await db.Events.FirstOrDefaultAsync(x => x.Id == eventId);
        if (race is null) return NotFound();
        var entries = await db.ManualPodiums.Where(x => x.EventId == eventId).ToListAsync();
        if (entries.Count == 0)
        {
            TempData["Error"] = "Cargá al menos un puesto antes de publicar los podios.";
            return RedirectToAction(nameof(ManualPodiums), new { eventId });
        }
        if (entries.Any(x => x.IsPublished))
        {
            TempData["Error"] = "Estos podios ya fueron publicados.";
            return RedirectToAction(nameof(ManualPodiums), new { eventId });
        }
        var now = DateTime.UtcNow;
        foreach (var entry in entries) { entry.IsPublished = true; entry.PublishedAt = now; }
        race.Status = EventStatus.Finished;
        await db.SaveChangesAsync();
        TempData["Success"] = "Podios manuales publicados. Ahora son visibles en la página de resultados.";
        return RedirectToAction(nameof(Event), new { slug = race.Slug });
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
        if (result is null) return NotFound();
        if (result.Status != ResultStatus.Official &&
            !User.IsInRole("Administrator") && !User.IsInRole("Organizer"))
            return NotFound();
        return View(result);
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

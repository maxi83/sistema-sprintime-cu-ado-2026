using lucho_sprintime.Data;
using lucho_sprintime.Models;
using lucho_sprintime.Services;
using lucho_sprintime.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

[Authorize(Roles = "Administrator,Organizer")]
public class AdminController(ApplicationDbContext db, IExportService exportService) : Controller
{
    public async Task<IActionResult> Dashboard(int? eventId = null)
    {
        var race = eventId.HasValue
            ? await db.Events.FirstOrDefaultAsync(x => x.Id == eventId.Value)
            : await db.Events.OrderByDescending(x => x.StartDate >= DateTime.Now).ThenBy(x => x.StartDate).FirstOrDefaultAsync();
        if (race is null) return RedirectToAction(nameof(EditEvent));

        var regs = db.Registrations.Where(x => x.EventId == race.Id);
        var model = new AdminDashboardViewModel
        {
            Event = race,
            Registrations = await regs.CountAsync(),
            ApprovedPayments = await regs.CountAsync(x => x.PaymentStatus == PaymentStatus.Approved),
            PendingPayments = await regs.CountAsync(x => x.PaymentStatus == PaymentStatus.Pending),
            Accredited = await regs.CountAsync(x => x.CheckInStatus != CheckInStatus.Pending),
            Revenue = await regs.Where(x => x.PaymentStatus == PaymentStatus.Approved).SumAsync(x => x.AmountPaid),
            LatestRegistrations = await regs.Include(x => x.Cyclist).Include(x => x.EventDistance).Include(x => x.Category).OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync()
        };
        ViewBag.Events = await db.Events.AsNoTracking().OrderByDescending(x => x.StartDate).ToListAsync();
        return View(model);
    }

    public async Task<IActionResult> Participants(int eventId, string? q = null, string? payment = null)
    {
        var race = await db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.Id == eventId);
        if (race is null) return NotFound();
        IQueryable<Registration> query = db.Registrations.AsNoTracking()
            .Where(x => x.EventId == eventId)
            .Include(x => x.Cyclist)
            .Include(x => x.EventDistance)
            .Include(x => x.Category);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Cyclist.FirstName.Contains(term) || x.Cyclist.LastName.Contains(term) || x.Cyclist.Dni.Contains(term) || (x.BibNumber ?? "").Contains(term));
        }
        if (Enum.TryParse<PaymentStatus>(payment, true, out var paymentStatus)) query = query.Where(x => x.PaymentStatus == paymentStatus);
        ViewBag.Event = race; ViewBag.Query = q; ViewBag.Payment = payment;
        return View(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignBib(int registrationId, string bibNumber)
    {
        var reg = await db.Registrations.FirstOrDefaultAsync(x => x.Id == registrationId);
        if (reg is null) return NotFound();
        bibNumber = (bibNumber ?? "").Trim();
        if (string.IsNullOrWhiteSpace(bibNumber)) { TempData["Error"] = "Ingresá un dorsal."; return RedirectToAction(nameof(Participants), new { eventId = reg.EventId }); }
        var exists = await db.Registrations.AnyAsync(x => x.EventId == reg.EventId && x.Id != reg.Id && x.BibNumber == bibNumber);
        if (exists) TempData["Error"] = "Ese dorsal ya está asignado.";
        else { reg.BibNumber = bibNumber; await db.SaveChangesAsync(); TempData["Success"] = "Dorsal actualizado."; }
        return RedirectToAction(nameof(Participants), new { eventId = reg.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int? id = null)
    {
        if (!id.HasValue)
            return View(new EventEditViewModel { StartDate = DateTime.Today.AddDays(30).AddHours(9), RegistrationOpenAt = DateTime.Today, RegistrationCloseAt = DateTime.Today.AddDays(28).AddHours(22) });
        var race = await db.Events.Include(x => x.Distances).ThenInclude(x => x.Categories).FirstOrDefaultAsync(x => x.Id == id.Value);
        if (race is null) return NotFound();
        return View(new EventEditViewModel
        {
            Id = race.Id, Name = race.Name, Slug = race.Slug, City = race.City, Province = race.Province, Venue = race.Venue,
            StartDate = race.StartDate, RegistrationOpenAt = race.RegistrationOpenAt, RegistrationCloseAt = race.RegistrationCloseAt,
            Description = race.Description, HeroImageUrl = race.HeroImageUrl, RegulationUrl = race.RegulationUrl, GpxUrl = race.GpxUrl,
            ElevationGainMeters = race.ElevationGainMeters, Status = race.Status, ShowPublicParticipants = race.ShowPublicParticipants,
            AllowOnsiteRegistration = race.AllowOnsiteRegistration,
            Distances = race.Distances.Select(d => new EventEditViewModel.DistanceInput { Id = d.Id, Name = d.Name, Kilometers = d.Kilometers, Price = d.Price, MaxParticipants = d.MaxParticipants, Categories = string.Join(", ", d.Categories.Select(c => c.Name)) }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventEditViewModel model)
    {
        if (model.RegistrationCloseAt >= model.StartDate) ModelState.AddModelError(nameof(model.RegistrationCloseAt), "El cierre debe ser anterior al inicio de la carrera.");
        if (await db.Events.AnyAsync(x => x.Slug == model.Slug && x.Id != model.Id)) ModelState.AddModelError(nameof(model.Slug), "Ya existe una carrera con ese slug.");
        model.Distances = model.Distances.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
        if (model.Distances.Count == 0) ModelState.AddModelError(nameof(model.Distances), "Agregá al menos una distancia.");
        if (!ModelState.IsValid) return View(model);

        Event race;
        if (model.Id.HasValue)
        {
            race = await db.Events.Include(x => x.Distances).ThenInclude(x => x.Categories).FirstAsync(x => x.Id == model.Id.Value);
        }
        else
        {
            race = new Event(); db.Events.Add(race);
        }
        race.Name = model.Name.Trim(); race.Slug = model.Slug.Trim().ToLowerInvariant(); race.City = model.City.Trim(); race.Province = model.Province.Trim(); race.Venue = model.Venue.Trim();
        race.StartDate = model.StartDate; race.RegistrationOpenAt = model.RegistrationOpenAt; race.RegistrationCloseAt = model.RegistrationCloseAt; race.Description = model.Description.Trim();
        race.HeroImageUrl = model.HeroImageUrl; race.RegulationUrl = model.RegulationUrl; race.GpxUrl = model.GpxUrl; race.ElevationGainMeters = model.ElevationGainMeters; race.Status = model.Status;
        race.ShowPublicParticipants = model.ShowPublicParticipants; race.AllowOnsiteRegistration = model.AllowOnsiteRegistration;

        foreach (var input in model.Distances)
        {
            var distance = input.Id.HasValue ? race.Distances.FirstOrDefault(x => x.Id == input.Id.Value) : null;
            if (distance is null) { distance = new EventDistance(); race.Distances.Add(distance); }
            distance.Name = input.Name.Trim(); distance.Kilometers = input.Kilometers; distance.Price = input.Price; distance.MaxParticipants = input.MaxParticipants;
            var names = input.Categories.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var old in distance.Categories.Where(x => !names.Contains(x.Name, StringComparer.OrdinalIgnoreCase)).ToList())
            {
                if (!await db.Registrations.AnyAsync(x => x.CategoryId == old.Id)) db.Categories.Remove(old);
            }
            foreach (var name in names.Where(name => !distance.Categories.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))) distance.Categories.Add(new Category { Name = name, Gender = "Mixto" });
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Carrera guardada.";
        return RedirectToAction(nameof(Dashboard), new { eventId = race.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseRegistration(int eventId)
    {
        var race = await db.Events.FirstOrDefaultAsync(x => x.Id == eventId);
        if (race is null) return NotFound();
        race.RegistrationCloseAt = DateTime.Now; race.Status = EventStatus.RegistrationClosed;
        await db.SaveChangesAsync(); TempData["Success"] = "Inscripciones cerradas. Ya podés exportar la nómina oficial.";
        return RedirectToAction(nameof(Dashboard), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Export(int eventId, string format = "pdf")
    {
        var race = await db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.Id == eventId);
        if (race is null) return NotFound();
        var regs = await db.Registrations.AsNoTracking().Where(x => x.EventId == eventId && x.PaymentStatus == PaymentStatus.Approved && x.Status == RegistrationStatus.Confirmed)
            .Include(x => x.Cyclist).Include(x => x.EventDistance).Include(x => x.Category).ToListAsync();
        var safeName = new string(race.Slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return format.ToLowerInvariant() switch
        {
            "csv" => File(exportService.BuildCsv(race, regs), "text/csv; charset=utf-8", $"{safeName}-inscriptos.csv"),
            "txt" => File(exportService.BuildTxt(race, regs), "text/plain; charset=utf-8", $"{safeName}-inscriptos.txt"),
            _ => File(exportService.BuildPdf(race, regs), "application/pdf", $"{safeName}-inscriptos.pdf")
        };
    }
}

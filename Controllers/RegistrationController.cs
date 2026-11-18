using lucho_sprintime.Data;
using lucho_sprintime.Models;
using lucho_sprintime.Services;
using lucho_sprintime.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class RegistrationController(ApplicationDbContext db, IMercadoPagoService mercadoPago, IWebHostEnvironment env) : Controller
{
    [Authorize(Roles = "Cyclist"), HttpGet]
    public async Task<IActionResult> Create(int eventId, int? distanceId = null)
    {
        var race = await LoadEvent(eventId);
        if (race is null) return NotFound();
        if (!IsRegistrationOpen(race)) return View("Closed", race);
        ViewBag.Event = race;
        var cyclist = await CurrentCyclist();
        if (cyclist is null) return Challenge();
        ConfigureAllowedCategories(race, cyclist);
        var selectedDistance = distanceId ?? race.Distances.FirstOrDefault()?.Id ?? 0;
        var initialCategory = race.Distances.FirstOrDefault(d => d.Id == selectedDistance)?.Categories
            .FirstOrDefault(c => AllowedCategories(race, cyclist, selectedDistance).Contains(c.Id) && !c.IsSelectableByCyclist);
        return View(new RegistrationInputModel { EventId = eventId, CategoryId = initialCategory?.Id ?? 0, EventDistanceId = selectedDistance,
            FirstName = cyclist.FirstName, LastName = cyclist.LastName, Dni = cyclist.Dni, Email = cyclist.Email,
            Phone = cyclist.Phone, BirthDate = cyclist.BirthDate, City = cyclist.City, Team = cyclist.Team,
            EmergencyContact = cyclist.EmergencyContact, EmergencyPhone = cyclist.EmergencyPhone });
    }

    [Authorize(Roles = "Cyclist"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegistrationInputModel model)
    {
        var race = await LoadEvent(model.EventId);
        if (race is null) return NotFound();
        ViewBag.Event = race;
        if (!IsRegistrationOpen(race)) return View("Closed", race);

        var cyclist = await CurrentCyclist();
        if (cyclist is null) return Challenge();
        model.FirstName = cyclist.FirstName; model.LastName = cyclist.LastName; model.Dni = cyclist.Dni;
        model.Email = cyclist.Email; model.Phone = cyclist.Phone; model.BirthDate = cyclist.BirthDate;
        model.City = cyclist.City; model.Team = cyclist.Team; model.EmergencyContact = cyclist.EmergencyContact;
        model.EmergencyPhone = cyclist.EmergencyPhone;
        ModelState.Clear();
        TryValidateModel(model);
        var distance = race.Distances.FirstOrDefault(x => x.Id == model.EventDistanceId);
        var category = distance?.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
        if (distance is null) ModelState.AddModelError(nameof(model.EventDistanceId), "La distancia no corresponde a esta carrera.");
        if (category is null) ModelState.AddModelError(nameof(model.CategoryId), "La categoría no corresponde a la distancia elegida.");
        else if (!AllowedCategories(race, cyclist, model.EventDistanceId).Contains(category.Id))
            ModelState.AddModelError(nameof(model.CategoryId), "La categoría no corresponde a tu edad y sexo para el día de la carrera.");
        ConfigureAllowedCategories(race, cyclist);
        if (distance?.MaxParticipants is int max)
        {
            var count = await db.Registrations.CountAsync(x => x.EventDistanceId == distance.Id && x.Status != RegistrationStatus.Cancelled && x.Status != RegistrationStatus.Refunded);
            if (count >= max) ModelState.AddModelError(nameof(model.EventDistanceId), "Esta distancia ya completó su cupo.");
        }
        if (!ModelState.IsValid) return View(model);

        var existing = await db.Registrations.AsNoTracking().FirstOrDefaultAsync(x => x.EventId == race.Id && x.CyclistId == cyclist.Id);
        if (existing is not null)
        {
            TempData["Info"] = "Ya existe una inscripción para este DNI/email en esta carrera.";
            return RedirectToAction(nameof(Status), new { id = existing.Id });
        }

        var registration = new Registration
        {
            EventId = race.Id, EventDistanceId = distance!.Id, CategoryId = category!.Id, CyclistId = cyclist.Id,
            AmountDue = distance.Price, Status = RegistrationStatus.PendingPayment, PaymentStatus = PaymentStatus.Pending
        };
        db.Registrations.Add(registration);
        await db.SaveChangesAsync();

        if (distance.Price <= 0)
        {
            registration.Status = RegistrationStatus.Confirmed;
            registration.PaymentStatus = PaymentStatus.Approved;
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Status), new { id = registration.Id });
        }

        await db.Entry(registration).Reference(x => x.Cyclist).LoadAsync();
        await db.Entry(registration).Reference(x => x.EventDistance).LoadAsync();
        registration.Event = race;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var checkout = await mercadoPago.CreateCheckoutAsync(
            registration,
            $"{baseUrl}/Registration/PaymentReturn?id={registration.Id}&state=success",
            $"{baseUrl}/Registration/PaymentReturn?id={registration.Id}&state=pending",
            $"{baseUrl}/Registration/PaymentReturn?id={registration.Id}&state=failure",
            $"{baseUrl}/MercadoPago/Webhook");

        var payment = new Payment { RegistrationId = registration.Id, Amount = registration.AmountDue, Status = PaymentStatus.Pending, PreferenceId = checkout.PreferenceId };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(checkout.CheckoutUrl)) return Redirect(checkout.CheckoutUrl);
        TempData["Info"] = checkout.IsConfigured
            ? "La inscripción quedó creada, pero Mercado Pago no devolvió un checkout. Revisá la configuración del panel."
            : "Inscripción creada. Mercado Pago está en modo local hasta configurar el AccessToken.";
        return RedirectToAction(nameof(Status), new { id = registration.Id });
    }

    [Authorize(Roles = "Cyclist,Administrator,Organizer")]
    public async Task<IActionResult> Status(int id)
    {
        var registration = await db.Registrations.AsNoTracking()
            .Include(x => x.Event).Include(x => x.EventDistance).Include(x => x.Category).Include(x => x.Cyclist).Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (registration is null || !CanAccess(registration.CyclistId)) return NotFound();
        ViewBag.IsDevelopment = env.IsDevelopment();
        return View(registration);
    }

    [Authorize(Roles = "Cyclist")]
    public IActionResult PaymentReturn(int id, string state)
    {
        TempData["Info"] = state == "success"
            ? "Mercado Pago recibió el pago. SprinTime confirma el cupo solamente cuando llega y se verifica la notificación del servidor."
            : state == "pending" ? "El pago quedó pendiente. SprinTime actualizará el estado automáticamente." : "El pago no fue aprobado. Podés volver a intentarlo.";
        return RedirectToAction(nameof(Status), new { id });
    }

    [Authorize(Roles = "Administrator"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoApprove(int id, int? returnEventId = null)
    {
        if (!env.IsDevelopment()) return NotFound();
        var r = await db.Registrations.Include(x => x.Payment).FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();
        r.PaymentStatus = PaymentStatus.Approved; r.Status = RegistrationStatus.Confirmed; r.AmountPaid = r.AmountDue; r.PaidAt = DateTime.UtcNow;
        if (r.Payment is not null) { r.Payment.Status = PaymentStatus.Approved; r.Payment.UpdatedAt = DateTime.UtcNow; r.Payment.ExternalId = $"DEMO-{id}"; }
        await db.SaveChangesAsync();
        TempData["Success"] = "Pago de desarrollo aprobado. El cupo quedó confirmado.";
        if (returnEventId == r.EventId) return RedirectToAction("Participants", "Admin", new { eventId = r.EventId });
        return RedirectToAction(nameof(Status), new { id });
    }

    [Authorize(Roles = "Cyclist,Administrator,Organizer"), HttpGet]
    public async Task<IActionResult> RacePass(int id)
    {
        var registration = await db.Registrations.AsNoTracking().Include(x => x.Event).Include(x => x.EventDistance).Include(x => x.Category).Include(x => x.Cyclist).FirstOrDefaultAsync(x => x.Id == id);
        if (registration is null || !CanAccess(registration.CyclistId) || registration.PaymentStatus != PaymentStatus.Approved) return NotFound();
        return View(registration);
    }

    private static HashSet<int> AllowedCategories(Event race, Cyclist cyclist, int distanceId)
    {
        var distance = race.Distances.FirstOrDefault(d => d.Id == distanceId);
        if (distance is null) return [];
        var day = race.StartDate.Date;
        var age = day.Year - cyclist.BirthDate.Year;
        if (cyclist.BirthDate.Date > day.AddYears(-age)) age--;
        if (age < 0 || string.IsNullOrWhiteSpace(cyclist.Gender)) return [];
        var hasRules = distance.Categories.Any(c => c.IsSelectableByCyclist || c.MinAge.HasValue ||
            c.MaxAge.HasValue || c.Gender != "Mixto");
        return distance.Categories.Where(c =>
            (!hasRules || c.IsSelectableByCyclist || c.MinAge.HasValue || c.MaxAge.HasValue || c.Gender != "Mixto") &&
            (c.Gender == "Mixto" || c.Gender == cyclist.Gender) &&
            (c.IsSelectableByCyclist ||
                ((!c.MinAge.HasValue || age >= c.MinAge) && (!c.MaxAge.HasValue || age <= c.MaxAge))))
            .Select(c => c.Id).ToHashSet();
    }

    private void ConfigureAllowedCategories(Event race, Cyclist cyclist)
    {
        ViewBag.AllowedCategories = race.Distances.SelectMany(d => AllowedCategories(race, cyclist, d.Id))
            .ToHashSet();
        ViewBag.CategoryGenderMissing = string.IsNullOrWhiteSpace(cyclist.Gender);
    }

    private async Task<Cyclist?> CurrentCyclist()
    {
        if (!User.IsInRole("Cyclist") || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return null;
        return await db.Cyclists.FirstOrDefaultAsync(x => x.Id == id);
    }
    private bool CanAccess(int cyclistId) => User.IsInRole("Administrator") || User.IsInRole("Organizer") ||
        (User.IsInRole("Cyclist") && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id == cyclistId);

    private Task<Event?> LoadEvent(int id) => db.Events.Include(x => x.Distances).ThenInclude(x => x.Categories).FirstOrDefaultAsync(x => x.Id == id);
    private static bool IsRegistrationOpen(Event race) => DateTime.Now >= race.RegistrationOpenAt && DateTime.Now <= race.RegistrationCloseAt && race.Status is EventStatus.Published or EventStatus.RegistrationOpen;
}

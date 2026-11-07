using lucho_sprintime.Data;
using lucho_sprintime.Models;
using lucho_sprintime.Services;
using lucho_sprintime.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class RegistrationController(ApplicationDbContext db, IMercadoPagoService mercadoPago, IWebHostEnvironment env) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(int eventId, int? distanceId = null)
    {
        var race = await LoadEvent(eventId);
        if (race is null) return NotFound();
        if (!IsRegistrationOpen(race)) return View("Closed", race);
        ViewBag.Event = race;
        return View(new RegistrationInputModel { EventId = eventId, EventDistanceId = distanceId ?? race.Distances.FirstOrDefault()?.Id ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegistrationInputModel model)
    {
        var race = await LoadEvent(model.EventId);
        if (race is null) return NotFound();
        ViewBag.Event = race;
        if (!IsRegistrationOpen(race)) return View("Closed", race);

        var distance = race.Distances.FirstOrDefault(x => x.Id == model.EventDistanceId);
        var category = distance?.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
        if (distance is null) ModelState.AddModelError(nameof(model.EventDistanceId), "La distancia no corresponde a esta carrera.");
        if (category is null) ModelState.AddModelError(nameof(model.CategoryId), "La categoría no corresponde a la distancia elegida.");
        if (distance?.MaxParticipants is int max)
        {
            var count = await db.Registrations.CountAsync(x => x.EventDistanceId == distance.Id && x.Status != RegistrationStatus.Cancelled && x.Status != RegistrationStatus.Refunded);
            if (count >= max) ModelState.AddModelError(nameof(model.EventDistanceId), "Esta distancia ya completó su cupo.");
        }
        if (!ModelState.IsValid) return View(model);

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var dni = model.Dni.Trim();
        var cyclist = await db.Cyclists.FirstOrDefaultAsync(x => x.Dni == dni || x.Email == normalizedEmail);
        if (cyclist is null)
        {
            cyclist = new Cyclist
            {
                FirstName = model.FirstName.Trim(), LastName = model.LastName.Trim(), Dni = dni, Email = normalizedEmail,
                Phone = model.Phone.Trim(), BirthDate = model.BirthDate!.Value, City = model.City.Trim(), Team = model.Team.Trim(),
                EmergencyContact = model.EmergencyContact.Trim(), EmergencyPhone = model.EmergencyPhone.Trim()
            };
            db.Cyclists.Add(cyclist);
            await db.SaveChangesAsync();
        }
        else
        {
            cyclist.FirstName = model.FirstName.Trim(); cyclist.LastName = model.LastName.Trim(); cyclist.Email = normalizedEmail;
            cyclist.Phone = model.Phone.Trim(); cyclist.BirthDate = model.BirthDate!.Value; cyclist.City = model.City.Trim(); cyclist.Team = model.Team.Trim();
            cyclist.EmergencyContact = model.EmergencyContact.Trim(); cyclist.EmergencyPhone = model.EmergencyPhone.Trim();
        }

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

    public async Task<IActionResult> Status(int id)
    {
        var registration = await db.Registrations.AsNoTracking()
            .Include(x => x.Event).Include(x => x.EventDistance).Include(x => x.Category).Include(x => x.Cyclist).Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (registration is null) return NotFound();
        ViewBag.IsDevelopment = env.IsDevelopment();
        return View(registration);
    }

    public IActionResult PaymentReturn(int id, string state)
    {
        TempData["Info"] = state == "success"
            ? "Mercado Pago recibió el pago. SprinTime confirma el cupo solamente cuando llega y se verifica la notificación del servidor."
            : state == "pending" ? "El pago quedó pendiente. SprinTime actualizará el estado automáticamente." : "El pago no fue aprobado. Podés volver a intentarlo.";
        return RedirectToAction(nameof(Status), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoApprove(int id)
    {
        if (!env.IsDevelopment()) return NotFound();
        var r = await db.Registrations.Include(x => x.Payment).FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();
        r.PaymentStatus = PaymentStatus.Approved; r.Status = RegistrationStatus.Confirmed; r.AmountPaid = r.AmountDue; r.PaidAt = DateTime.UtcNow;
        if (r.Payment is not null) { r.Payment.Status = PaymentStatus.Approved; r.Payment.UpdatedAt = DateTime.UtcNow; r.Payment.ExternalId = $"DEMO-{id}"; }
        await db.SaveChangesAsync();
        TempData["Success"] = "Pago de desarrollo aprobado. El cupo quedó confirmado.";
        return RedirectToAction(nameof(Status), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> RacePass(int id)
    {
        var registration = await db.Registrations.AsNoTracking().Include(x => x.Event).Include(x => x.EventDistance).Include(x => x.Category).Include(x => x.Cyclist).FirstOrDefaultAsync(x => x.Id == id);
        if (registration is null || registration.PaymentStatus != PaymentStatus.Approved) return NotFound();
        return View(registration);
    }

    private Task<Event?> LoadEvent(int id) => db.Events.Include(x => x.Distances).ThenInclude(x => x.Categories).FirstOrDefaultAsync(x => x.Id == id);
    private static bool IsRegistrationOpen(Event race) => DateTime.Now >= race.RegistrationOpenAt && DateTime.Now <= race.RegistrationCloseAt && race.Status is EventStatus.Published or EventStatus.RegistrationOpen;
}

using lucho_sprintime.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class CyclistController(ApplicationDbContext db) : Controller
{
    [Authorize(Roles = "Cyclist"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGender(string? gender)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var cyclistId)) return Challenge();
        if (gender is not ("Masculino" or "Femenino"))
        {
            TempData["Error"] = "Seleccioná una opción válida para la categoría de competencia.";
            return RedirectToAction(nameof(Profile));
        }
        var cyclist = await db.Cyclists.FirstOrDefaultAsync(x => x.Id == cyclistId);
        if (cyclist is null) return NotFound();
        cyclist.Gender = gender;
        await db.SaveChangesAsync();
        TempData["Success"] = "Dato actualizado para asignar tu categoría.";
        return RedirectToAction(nameof(Profile));
    }


    [Authorize(Roles = "Cyclist"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBib(string bibNumber)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var cyclistId)) return Challenge();
        var cyclist = await db.Cyclists.FirstOrDefaultAsync(x => x.Id == cyclistId);
        if (cyclist is null) return NotFound();
        if (!int.TryParse(bibNumber, out var number) || number < 1 || number > 200)
        {
            TempData["Error"] = "Elegí un dorsal de SprinTime entre 1 y 200.";
            return RedirectToAction(nameof(Profile));
        }
        var bib = number.ToString();
        if (await db.Cyclists.AnyAsync(x => x.Id != cyclistId && x.SprintimeBib == bib) ||
            await db.Registrations.AnyAsync(x => x.CyclistId != cyclistId && x.BibNumber == bib))
        {
            TempData["Error"] = "Ese dorsal ya está en uso.";
            return RedirectToAction(nameof(Profile));
        }
        var registrations = await db.Registrations.Where(x => x.CyclistId == cyclistId).ToListAsync();
        cyclist.SprintimeBib = bib;
        foreach (var registration in registrations) registration.BibNumber = bib;
        try
        {
            await db.SaveChangesAsync();
            TempData["Success"] = "Tu dorsal permanente quedó guardado.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "No se pudo asignar el dorsal: puede estar ocupado en otra inscripción.";
        }
        return RedirectToAction(nameof(Profile));
    }

    [Authorize(Roles = "Cyclist"), HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var cyclistId)) return Challenge();
        var cyclist = await db.Cyclists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cyclistId);
        return cyclist is null ? NotFound() : View(cyclist);
    }

    [Authorize(Roles = "Cyclist")]
    public async Task<IActionResult> Dashboard()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var cyclistId)) return Challenge();
        var cyclist = await db.Cyclists.AsNoTracking().Include(x => x.Bicycles).Include(x => x.Registrations).ThenInclude(x => x.Event).Include(x => x.Registrations).ThenInclude(x => x.EventDistance).Include(x => x.Registrations).ThenInclude(x => x.Result).FirstOrDefaultAsync(x => x.Id == cyclistId);
        return cyclist is null ? NotFound() : View(cyclist);
    }
}

public class QrController(lucho_sprintime.Services.IQrCodeService qrCodeService) : Controller
{
    [HttpGet("qr/{token}")]
    public IActionResult Index(string token)
        => File(qrCodeService.GeneratePng(token), "image/png");
}

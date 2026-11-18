using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using lucho_sprintime.Data;
using lucho_sprintime.Models;
using lucho_sprintime.ViewModels;

namespace lucho_sprintime.Controllers;

public class AccountController(IConfiguration configuration, ApplicationDbContext db, IPasswordHasher<Cyclist> passwordHasher) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string? email, string? password, string? returnUrl = null)
    {
        var expectedEmail = configuration["AdminAuth:Email"];
        var expectedPassword = configuration["AdminAuth:Password"];
        var normalizedEmail = email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expectedEmail) || string.IsNullOrWhiteSpace(expectedPassword) ||
            !string.Equals(normalizedEmail, expectedEmail, StringComparison.OrdinalIgnoreCase) || password != expectedPassword)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Error = "Credenciales incorrectas.";
            return View();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, normalizedEmail),
            new Claim(ClaimTypes.Role, "Administrator"),
            new Claim("display_name", "Organizador SprinTime")
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction("Dashboard", "Admin");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }


    [AllowAnonymous, HttpGet]
    public IActionResult Register(string? returnUrl = null) => View(new CyclistAccountInput { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(CyclistAccountInput model)
    {
        if (model.BirthDate > DateTime.Today || model.BirthDate < new DateTime(1900, 1, 1))
            ModelState.AddModelError(nameof(model.BirthDate), "Fecha de nacimiento inválida.");
        var normalizedEmail = (model.Email ?? "").Trim().ToLowerInvariant();
        var dni = (model.Dni ?? "").Trim();
        if (await db.Cyclists.AnyAsync(x => x.Email == normalizedEmail || x.Dni == dni))
            ModelState.AddModelError(nameof(model.Email), "Ya existe una cuenta o un corredor con este DNI o correo. Contactá al organizador si necesitás recuperar un perfil anterior.");
        if (!ModelState.IsValid) return View(model);
        if (model.Gender is not ("Masculino" or "Femenino")) ModelState.AddModelError(nameof(model.Gender), "Seleccioná el sexo para la categoría.");
        if (!ModelState.IsValid) return View(model);
        var cyclist = new Cyclist
        {
            FirstName = model.FirstName.Trim(), LastName = model.LastName.Trim(), Dni = dni, Email = normalizedEmail,
            Phone = model.Phone.Trim(), BirthDate = model.BirthDate!.Value, Gender = model.Gender, City = model.City.Trim(), Team = model.Team.Trim(),
            EmergencyContact = model.EmergencyContact.Trim(), EmergencyPhone = model.EmergencyPhone.Trim(),
            SecondEmergencyPhone = model.SecondEmergencyPhone?.Trim(), BloodType = model.BloodType,
            HasInsurance = model.HasInsurance, InsuranceNumber = model.HasInsurance ? model.InsuranceNumber?.Trim() : null,
            MedicalConditions = model.MedicalConditions?.Trim(), MedicalNotes = model.MedicalNotes?.Trim(), MedicalConsent = model.MedicalConsent
        };
        cyclist.PasswordHash = passwordHasher.HashPassword(cyclist, model.Password);
        db.Cyclists.Add(cyclist);
        await db.SaveChangesAsync();
        await SignInCyclist(cyclist);
        return SafeReturn(model.ReturnUrl);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult CyclistLogin(string? returnUrl = null) { ViewBag.ReturnUrl = returnUrl; return View(); }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CyclistLogin(string? email, string? password, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        var normalizedEmail = (email ?? "").Trim().ToLowerInvariant();
        var cyclist = await db.Cyclists.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        if (cyclist is null || string.IsNullOrWhiteSpace(cyclist.PasswordHash) ||
            passwordHasher.VerifyHashedPassword(cyclist, cyclist.PasswordHash, password ?? "") == PasswordVerificationResult.Failed)
        {
            ViewBag.Error = "Datos de acceso incorrectos.";
            return View();
        }
        await SignInCyclist(cyclist);
        return SafeReturn(returnUrl);
    }

    private async Task SignInCyclist(Cyclist cyclist)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, cyclist.Id.ToString()),
            new Claim(ClaimTypes.Name, cyclist.Email),
            new Claim(ClaimTypes.Role, "Cyclist"),
            new Claim("display_name", cyclist.FirstName)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    }

    private IActionResult SafeReturn(string? returnUrl)
        => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Dashboard", "Cyclist");

    public IActionResult AccessDenied() => View();
}

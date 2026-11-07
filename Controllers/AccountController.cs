using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lucho_sprintime.Controllers;

public class AccountController(IConfiguration configuration) : Controller
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

    public IActionResult AccessDenied() => View();
}

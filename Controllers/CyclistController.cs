using lucho_sprintime.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class CyclistController(ApplicationDbContext db) : Controller
{
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

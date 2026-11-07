using lucho_sprintime.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class CyclistController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Dashboard(int? id = null)
    {
        var cyclist = id.HasValue
            ? await db.Cyclists.AsNoTracking().Include(x => x.Bicycles).Include(x => x.Registrations).ThenInclude(x => x.Event).Include(x => x.Registrations).ThenInclude(x => x.Result).FirstOrDefaultAsync(x => x.Id == id)
            : await db.Cyclists.AsNoTracking().Include(x => x.Bicycles).Include(x => x.Registrations).ThenInclude(x => x.Event).Include(x => x.Registrations).ThenInclude(x => x.Result).FirstOrDefaultAsync();
        return cyclist is null ? NotFound() : View(cyclist);
    }
}

public class QrController(lucho_sprintime.Services.IQrCodeService qrCodeService) : Controller
{
    [HttpGet("qr/{token}")]
    public IActionResult Index(string token)
        => File(qrCodeService.GeneratePng(token), "image/png");
}

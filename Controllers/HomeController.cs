using System.Diagnostics;
using lucho_sprintime.Data;
using lucho_sprintime.Models;
using lucho_sprintime.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class HomeController(ApplicationDbContext db, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var events = await db.Events.AsNoTracking().Include(x => x.Distances).OrderBy(x => x.StartDate).ToListAsync();
        var model = new HomeViewModel
        {
            FeaturedEvent = events.FirstOrDefault(x => x.StartDate >= now && x.Status != EventStatus.Draft),
            UpcomingEvents = events.Where(x => x.StartDate >= now && x.Status != EventStatus.Draft).Take(6).ToList(),
            FinishedEvents = events.Where(x => x.StartDate < now || x.Status == EventStatus.Finished).OrderByDescending(x => x.StartDate).Take(3).ToList()
        };
        return View(model);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        logger.LogError("Unhandled error. Trace: {Trace}", HttpContext.TraceIdentifier);
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

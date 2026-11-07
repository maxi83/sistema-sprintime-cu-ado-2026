using lucho_sprintime.Services;
using Microsoft.AspNetCore.Mvc;

namespace lucho_sprintime.Controllers;

public class RankingController(IRankingService rankingService) : Controller
{
    public async Task<IActionResult> Index() => View(await rankingService.BuildAsync());
}

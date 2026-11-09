using lucho_sprintime.Services;
using Microsoft.AspNetCore.Mvc;

namespace lucho_sprintime.Controllers;

public class RankingController(IRankingService rankingService) : Controller
{
    // El ranking por campeonato se habilitará cuando el organizador configure
    // las carreras puntuables y el reglamento completo de puntos.
    public IActionResult Index() => NotFound();
}

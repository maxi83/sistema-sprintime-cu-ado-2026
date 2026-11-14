using lucho_sprintime.Models;
namespace lucho_sprintime.Data;

// Las carreras de prueba no deben aparecer ni insertarse en una instalación real.
public static class SeedData
{
    public static Task InitializeAsync(ApplicationDbContext db) => Task.CompletedTask;
}

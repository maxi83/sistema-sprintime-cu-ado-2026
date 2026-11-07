using lucho_sprintime.Data;
using lucho_sprintime.Models;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Services;

public class RegistrationClosureHostedService(IServiceScopeFactory scopeFactory, ILogger<RegistrationClosureHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.Now;
                var eventsToClose = await db.Events
                    .Where(x => x.Status == EventStatus.RegistrationOpen && x.RegistrationCloseAt <= now)
                    .ToListAsync(stoppingToken);
                if (eventsToClose.Count > 0)
                {
                    foreach (var race in eventsToClose) race.Status = EventStatus.RegistrationClosed;
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("SprinTime cerró automáticamente {Count} ventana(s) de inscripción.", eventsToClose.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al verificar el cierre automático de inscripciones.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

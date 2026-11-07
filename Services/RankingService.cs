using lucho_sprintime.Data;
using lucho_sprintime.Models;
using lucho_sprintime.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Services;

public class RankingService(ApplicationDbContext db) : IRankingService
{
    private static readonly int[] PositionPoints = [100, 85, 75, 65, 55, 45, 40, 35, 30, 25];

    public async Task<List<RankingRowViewModel>> BuildAsync(int? eventId = null, CancellationToken ct = default)
    {
        var query = db.Results
            .AsNoTracking()
            .Include(x => x.Registration).ThenInclude(x => x.Cyclist)
            .Include(x => x.Registration).ThenInclude(x => x.Category)
            .Where(x => x.Status == ResultStatus.Official || x.Status == ResultStatus.Provisional);
        if (eventId.HasValue) query = query.Where(x => x.Registration.EventId == eventId.Value);
        var results = await query.ToListAsync(ct);

        var rows = results.GroupBy(x => new { x.Registration.CyclistId, x.Registration.CategoryId })
            .Select(g => new RankingRowViewModel
            {
                CyclistName = $"{g.First().Registration.Cyclist.FirstName} {g.First().Registration.Cyclist.LastName}",
                Team = g.First().Registration.Cyclist.Team,
                Category = g.First().Registration.Category.Name,
                Races = g.Count(),
                Podiums = g.Count(x => x.CategoryPosition is <= 3 and > 0),
                Points = g.Sum(x => 10 + PointsFor(x.CategoryPosition))
            })
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.Podiums)
            .ThenBy(x => x.CyclistName)
            .ToList();
        for (var i = 0; i < rows.Count; i++) rows[i].Position = i + 1;
        return rows;
    }

    private static int PointsFor(int? position)
        => position is > 0 && position <= PositionPoints.Length ? PositionPoints[position.Value - 1] : 0;
}

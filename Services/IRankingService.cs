using lucho_sprintime.ViewModels;

namespace lucho_sprintime.Services;

public interface IRankingService
{
    Task<List<RankingRowViewModel>> BuildAsync(int? eventId = null, CancellationToken ct = default);
}

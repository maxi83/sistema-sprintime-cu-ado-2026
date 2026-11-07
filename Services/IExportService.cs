using lucho_sprintime.Models;

namespace lucho_sprintime.Services;

public interface IExportService
{
    byte[] BuildCsv(Event race, IReadOnlyCollection<Registration> registrations);
    byte[] BuildTxt(Event race, IReadOnlyCollection<Registration> registrations);
    byte[] BuildPdf(Event race, IReadOnlyCollection<Registration> registrations);
    byte[] BuildDiploma(Result result);
}

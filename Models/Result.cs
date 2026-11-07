namespace lucho_sprintime.Models;

public class Result
{
    public int Id { get; set; }
    public int RegistrationId { get; set; }
    public Registration Registration { get; set; } = null!;
    public TimeSpan? TotalTime { get; set; }
    public int? GeneralPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public ResultStatus Status { get; set; } = ResultStatus.Provisional;
    public DateTime? PublishedAt { get; set; }
}

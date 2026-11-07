using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class TimingRecord
{
    public int Id { get; set; }
    public int RegistrationId { get; set; }
    public Registration Registration { get; set; } = null!;
    public int CheckpointId { get; set; }
    public Checkpoint Checkpoint { get; set; } = null!;
    public DateTime TimestampUtc { get; set; }
    [MaxLength(80)] public string Source { get; set; } = "RaceDay";
    [MaxLength(80)] public string? ClientEntryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

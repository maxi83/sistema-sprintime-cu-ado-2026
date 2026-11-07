using lucho_sprintime.Models;

namespace lucho_sprintime.ViewModels;

public class RaceDayViewModel
{
    public Event Event { get; set; } = null!;
    public int Confirmed { get; set; }
    public int Accredited { get; set; }
    public int Started { get; set; }
    public int Finished { get; set; }
    public List<Checkpoint> Checkpoints { get; set; } = [];
    public List<Registration> Recent { get; set; } = [];
}

public class TimingEntryInput
{
    public int EventId { get; set; }
    public int CheckpointId { get; set; }
    public string BibNumber { get; set; } = "";
    public DateTime TimestampUtc { get; set; }
    public string ClientEntryId { get; set; } = "";
}

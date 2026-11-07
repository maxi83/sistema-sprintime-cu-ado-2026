using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class Checkpoint
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    [Required, MaxLength(80)] public string Name { get; set; } = "";
    public decimal Kilometer { get; set; }
    public int Order { get; set; }
    public TimingPointType Type { get; set; }
    public List<TimingRecord> TimingRecords { get; set; } = [];
}

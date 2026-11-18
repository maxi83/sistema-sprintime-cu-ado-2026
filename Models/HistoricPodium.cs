using System.ComponentModel.DataAnnotations;
namespace lucho_sprintime.Models;
public class HistoricPodium
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    [Required, MaxLength(120)] public string Category { get; set; } = "";
    [Range(1, 3)] public int Place { get; set; }
    [Required, MaxLength(160)] public string CyclistName { get; set; } = "";
}

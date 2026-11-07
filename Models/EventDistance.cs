using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class EventDistance
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    [Required, MaxLength(80)] public string Name { get; set; } = "";
    public decimal Kilometers { get; set; }
    public decimal Price { get; set; }
    public int? MaxParticipants { get; set; }
    public List<Category> Categories { get; set; } = [];
    public List<Registration> Registrations { get; set; } = [];
}

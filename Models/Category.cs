using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class Category
{
    public int Id { get; set; }
    public int EventDistanceId { get; set; }
    public EventDistance EventDistance { get; set; } = null!;
    [Required, MaxLength(80)] public string Name { get; set; } = "";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    [MaxLength(20)] public string Gender { get; set; } = "Mixto";
    public bool IsSelectableByCyclist { get; set; }
    public List<Registration> Registrations { get; set; } = [];
}

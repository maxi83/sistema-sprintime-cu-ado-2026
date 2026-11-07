using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class Bicycle
{
    public int Id { get; set; }
    public int CyclistId { get; set; }
    public Cyclist Cyclist { get; set; } = null!;
    [Required, MaxLength(80)] public string Name { get; set; } = "Mi bicicleta";
    [MaxLength(80)] public string Brand { get; set; } = "";
    [MaxLength(80)] public string Model { get; set; } = "";
    [MaxLength(40)] public string Type { get; set; } = "MTB";
    public decimal CurrentKilometers { get; set; }
    public int ServiceEveryKm { get; set; } = 500;
    public decimal KilometersSinceService { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class Sponsor
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(500)] public string? LogoUrl { get; set; }
    [MaxLength(500)] public string? WebsiteUrl { get; set; }
    public int Order { get; set; }
}

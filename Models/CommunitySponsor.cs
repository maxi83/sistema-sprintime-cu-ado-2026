using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class CommunitySponsor
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(600)] public string? Description { get; set; }
    [MaxLength(500)] public string? LogoUrl { get; set; }
    [MaxLength(500)] public string? LinkUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}

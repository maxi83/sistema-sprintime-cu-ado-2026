using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class NewsPost
{
    public int Id { get; set; }
    [Required, MaxLength(160)] public string Title { get; set; } = "";
    [Required, MaxLength(6000)] public string Body { get; set; } = "";
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace lucho_sprintime.ViewModels;

public class NewsPostInput
{
    public int? Id { get; set; }
    [Required, MaxLength(160)] public string Title { get; set; } = "";
    [Required, MaxLength(6000)] public string Body { get; set; } = "";
    public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
    public bool RemoveImage { get; set; }
    public bool IsPublished { get; set; }
}

public class CommunitySponsorInput
{
    public int? Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(600)] public string? Description { get; set; }
    [Url, MaxLength(500)] public string? LinkUrl { get; set; }
    public string? LogoUrl { get; set; }
    public IFormFile? LogoFile { get; set; }
    public bool RemoveLogo { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}

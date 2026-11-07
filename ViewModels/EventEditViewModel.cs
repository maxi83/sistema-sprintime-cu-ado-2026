using System.ComponentModel.DataAnnotations;
using lucho_sprintime.Models;

namespace lucho_sprintime.ViewModels;

public class EventEditViewModel
{
    public int? Id { get; set; }
    [Required, MaxLength(140)] public string Name { get; set; } = "";
    [Required, MaxLength(160)] public string Slug { get; set; } = "";
    [Required, MaxLength(100)] public string City { get; set; } = "";
    [Required, MaxLength(100)] public string Province { get; set; } = "";
    [MaxLength(180)] public string Venue { get; set; } = "";
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime RegistrationOpenAt { get; set; }
    [Required] public DateTime RegistrationCloseAt { get; set; }
    [MaxLength(2400)] public string Description { get; set; } = "";
    [MaxLength(500)] public string? HeroImageUrl { get; set; }
    [MaxLength(500)] public string? RegulationUrl { get; set; }
    [MaxLength(500)] public string? GpxUrl { get; set; }
    public int ElevationGainMeters { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public bool ShowPublicParticipants { get; set; } = true;
    public bool AllowOnsiteRegistration { get; set; }
    public List<DistanceInput> Distances { get; set; } = [new()];

    public class DistanceInput
    {
        public int? Id { get; set; }
        [Required] public string Name { get; set; } = "MTB 60K";
        [Range(0.1, 1000)] public decimal Kilometers { get; set; } = 60;
        [Range(0, 99999999)] public decimal Price { get; set; }
        public int? MaxParticipants { get; set; }
        public string Categories { get; set; } = "General";
    }
}

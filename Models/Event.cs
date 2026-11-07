using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class Event
{
    public int Id { get; set; }
    [Required, MaxLength(140)] public string Name { get; set; } = "";
    [Required, MaxLength(160)] public string Slug { get; set; } = "";
    [MaxLength(100)] public string City { get; set; } = "San Luis";
    [MaxLength(100)] public string Province { get; set; } = "San Luis";
    [MaxLength(180)] public string Venue { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime RegistrationOpenAt { get; set; }
    public DateTime RegistrationCloseAt { get; set; }
    [MaxLength(2400)] public string Description { get; set; } = "";
    [MaxLength(500)] public string? HeroImageUrl { get; set; }
    [MaxLength(500)] public string? RegulationUrl { get; set; }
    [MaxLength(500)] public string? GpxUrl { get; set; }
    public int ElevationGainMeters { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public bool ShowPublicParticipants { get; set; } = true;
    public bool AllowOnsiteRegistration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<EventDistance> Distances { get; set; } = [];
    public List<Sponsor> Sponsors { get; set; } = [];
    public List<Checkpoint> Checkpoints { get; set; } = [];
    public List<Registration> Registrations { get; set; } = [];
}

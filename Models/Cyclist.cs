using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class Cyclist
{
    public int Id { get; set; }
    [Required, MaxLength(80)] public string FirstName { get; set; } = "";
    [Required, MaxLength(80)] public string LastName { get; set; } = "";
    [Required, MaxLength(30)] public string Dni { get; set; } = "";
    [Required, EmailAddress, MaxLength(160)] public string Email { get; set; } = "";
    [MaxLength(40)] public string Phone { get; set; } = "";
    public DateTime BirthDate { get; set; }
    [MaxLength(100)] public string City { get; set; } = "";
    [MaxLength(120)] public string Team { get; set; } = "";
    [MaxLength(160)] public string EmergencyContact { get; set; } = "";
    [MaxLength(40)] public string EmergencyPhone { get; set; } = "";
    [MaxLength(500)] public string? PasswordHash { get; set; }
    [MaxLength(10)] public string? BloodType { get; set; }
    public bool HasInsurance { get; set; }
    [MaxLength(120)] public string? InsuranceNumber { get; set; }
    [MaxLength(40)] public string? SecondEmergencyPhone { get; set; }
    [MaxLength(1000)] public string? MedicalConditions { get; set; }
    [MaxLength(1000)] public string? MedicalNotes { get; set; }
    public bool MedicalConsent { get; set; }
    [MaxLength(20)] public string? SprintimeBib { get; set; }
    public List<Registration> Registrations { get; set; } = [];
    public List<Bicycle> Bicycles { get; set; } = [];
}

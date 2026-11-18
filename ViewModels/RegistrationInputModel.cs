using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.ViewModels;

public class RegistrationInputModel : IValidatableObject
{
    public int EventId { get; set; }
    [Required(ErrorMessage = "Elegí una distancia.")] public int EventDistanceId { get; set; }
    [Required(ErrorMessage = "Elegí una categoría.")] public int CategoryId { get; set; }
    [Required, MaxLength(80), Display(Name = "Nombre")] public string FirstName { get; set; } = "";
    [Required, MaxLength(80), Display(Name = "Apellido")] public string LastName { get; set; } = "";
    [Required, MaxLength(30), Display(Name = "DNI")] public string Dni { get; set; } = "";
    [Required, EmailAddress, MaxLength(160), Display(Name = "Email")] public string Email { get; set; } = "";
    [Required, MaxLength(40), Display(Name = "Teléfono")] public string Phone { get; set; } = "";
    [Required, DataType(DataType.Date), Display(Name = "Fecha de nacimiento")] public DateTime? BirthDate { get; set; }
    [MaxLength(100), Display(Name = "Ciudad")] public string City { get; set; } = "";
    [MaxLength(120), Display(Name = "Equipo / club")] public string Team { get; set; } = "";
    [Required, MaxLength(160), Display(Name = "Contacto de emergencia")] public string EmergencyContact { get; set; } = "";
    [Required, MaxLength(40), Display(Name = "Teléfono de emergencia")] public string EmergencyPhone { get; set; } = "";
    public bool AcceptTerms { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AcceptTerms)
            yield return new ValidationResult("Tenés que aceptar el reglamento y la declaración.", new[] { nameof(AcceptTerms) });
    }
}

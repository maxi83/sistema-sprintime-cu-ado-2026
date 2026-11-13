namespace lucho_sprintime.Models;

// Clasificación ingresada por dorsal y confirmada por el organizador.
// Se guarda aparte del cronometraje automático para no modificar sus registros.
public class ManualPodium
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int RegistrationId { get; set; }
    public Registration Registration { get; set; } = null!;
    public int Place { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
}

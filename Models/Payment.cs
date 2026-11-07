using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class Payment
{
    public int Id { get; set; }
    public int RegistrationId { get; set; }
    public Registration Registration { get; set; } = null!;
    [MaxLength(40)] public string Provider { get; set; } = "MercadoPago";
    [MaxLength(120)] public string? ExternalId { get; set; }
    [MaxLength(160)] public string? PreferenceId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? RawNotification { get; set; }
}

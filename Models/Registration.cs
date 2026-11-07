using System.ComponentModel.DataAnnotations;

namespace lucho_sprintime.Models;

public class Registration
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public int EventDistanceId { get; set; }
    public EventDistance EventDistance { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int CyclistId { get; set; }
    public Cyclist Cyclist { get; set; } = null!;
    [MaxLength(20)] public string? BibNumber { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.PendingPayment;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public CheckInStatus CheckInStatus { get; set; } = CheckInStatus.Pending;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    [MaxLength(120)] public string? PaymentReference { get; set; }
    [MaxLength(120)] public string? PaymentExternalId { get; set; }
    public Guid QrToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public Payment? Payment { get; set; }
    public List<TimingRecord> TimingRecords { get; set; } = [];
    public Result? Result { get; set; }
}

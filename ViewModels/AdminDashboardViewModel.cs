using lucho_sprintime.Models;

namespace lucho_sprintime.ViewModels;

public class AdminDashboardViewModel
{
    public Event Event { get; set; } = null!;
    public int Registrations { get; set; }
    public int ApprovedPayments { get; set; }
    public int PendingPayments { get; set; }
    public int Accredited { get; set; }
    public decimal Revenue { get; set; }
    public decimal PaymentRate => Registrations == 0 ? 0 : (decimal)ApprovedPayments / Registrations * 100;
    public List<Registration> LatestRegistrations { get; set; } = [];
}

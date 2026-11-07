using lucho_sprintime.Models;

namespace lucho_sprintime.ViewModels;

public class HomeViewModel
{
    public Event? FeaturedEvent { get; set; }
    public List<Event> UpcomingEvents { get; set; } = [];
    public List<Event> FinishedEvents { get; set; } = [];
}

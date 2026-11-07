namespace lucho_sprintime.ViewModels;

public class RankingRowViewModel
{
    public int Position { get; set; }
    public string CyclistName { get; set; } = "";
    public string Team { get; set; } = "";
    public string Category { get; set; } = "";
    public int Races { get; set; }
    public int Podiums { get; set; }
    public int Points { get; set; }
}

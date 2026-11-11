using lucho_sprintime.Models;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<HistoricPodium> HistoricPodiums => Set<HistoricPodium>();
    public DbSet<SocialLinks> SocialLinks => Set<SocialLinks>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventDistance> EventDistances => Set<EventDistance>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Cyclist> Cyclists => Set<Cyclist>();
    public DbSet<Bicycle> Bicycles => Set<Bicycle>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Sponsor> Sponsors => Set<Sponsor>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<TimingRecord> TimingRecords => Set<TimingRecord>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // This singleton configuration table uses a fixed key (Id = 1), not SQL IDENTITY.
        modelBuilder.Entity<SocialLinks>().Property(x => x.Id).ValueGeneratedNever();
        modelBuilder.Entity<Event>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<HistoricPodium>().HasIndex(x => new { x.EventId, x.Category, x.Place }).IsUnique();
        modelBuilder.Entity<Cyclist>().HasIndex(x => x.Dni).IsUnique();
        modelBuilder.Entity<Cyclist>().HasIndex(x => x.Email);
        modelBuilder.Entity<Cyclist>().HasIndex(x => x.SprintimeBib).IsUnique().HasFilter("[SprintimeBib] IS NOT NULL");
        modelBuilder.Entity<Registration>().HasIndex(x => new { x.EventId, x.CyclistId }).IsUnique();
        modelBuilder.Entity<Registration>().HasIndex(x => x.QrToken).IsUnique();
        modelBuilder.Entity<Registration>().HasIndex(x => new { x.EventId, x.BibNumber }).IsUnique().HasFilter("[BibNumber] IS NOT NULL");
        modelBuilder.Entity<TimingRecord>().HasIndex(x => x.ClientEntryId).IsUnique().HasFilter("[ClientEntryId] IS NOT NULL");
        modelBuilder.Entity<Payment>().HasIndex(x => x.RegistrationId).IsUnique();
        modelBuilder.Entity<Result>().HasIndex(x => x.RegistrationId).IsUnique();

        modelBuilder.Entity<EventDistance>().Property(x => x.Price).HasPrecision(18, 2);
        modelBuilder.Entity<EventDistance>().Property(x => x.Kilometers).HasPrecision(8, 2);
        modelBuilder.Entity<Registration>().Property(x => x.AmountDue).HasPrecision(18, 2);
        modelBuilder.Entity<Registration>().Property(x => x.AmountPaid).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Checkpoint>().Property(x => x.Kilometer).HasPrecision(8, 2);
        modelBuilder.Entity<Bicycle>().Property(x => x.CurrentKilometers).HasPrecision(10, 2);
        modelBuilder.Entity<Bicycle>().Property(x => x.KilometersSinceService).HasPrecision(10, 2);

        // SQL Server can reject this domain with multiple cascade paths (Event -> Distance -> Registration, etc.).
        // Race data is auditable, so destructive cascades are intentionally disabled.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
    }
}

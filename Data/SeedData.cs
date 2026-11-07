using lucho_sprintime.Models;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        if (await db.Events.AnyAsync()) return;

        var eventDate = DateTime.Today.AddDays(32).AddHours(9);
        var race = new Event
        {
            Name = "Desafío MTB Potrero 2026",
            Slug = "desafio-mtb-potrero-2026",
            City = "Potrero de los Funes",
            Province = "San Luis",
            Venue = "Costanera del Lago",
            StartDate = eventDate,
            RegistrationOpenAt = DateTime.Today.AddDays(-15),
            RegistrationCloseAt = eventDate.AddDays(-2).Date.AddHours(22),
            Description = "Una carrera de montaña pensada para competir, disfrutar el circuito y vivir toda la experiencia desde SprinTime.",
            HeroImageUrl = "/images/hero-race.svg",
            ElevationGainMeters = 1240,
            Status = EventStatus.RegistrationOpen,
            AllowOnsiteRegistration = true,
            ShowPublicParticipants = true
        };

        var d60 = new EventDistance { Name = "MTB 60K", Kilometers = 60, Price = 25000, MaxParticipants = 500 };
        d60.Categories.AddRange([
            new Category { Name = "Elite", MinAge = 18, MaxAge = 29, Gender = "Mixto" },
            new Category { Name = "Master A", MinAge = 30, MaxAge = 39, Gender = "Mixto" },
            new Category { Name = "Master B", MinAge = 40, MaxAge = 49, Gender = "Mixto" },
            new Category { Name = "Master C", MinAge = 50, Gender = "Mixto" }
        ]);
        var d35 = new EventDistance { Name = "MTB 35K", Kilometers = 35, Price = 19000, MaxParticipants = 300 };
        d35.Categories.AddRange([
            new Category { Name = "General", MinAge = 16, Gender = "Mixto" },
            new Category { Name = "Promocional", MinAge = 16, Gender = "Mixto" }
        ]);
        race.Distances.AddRange([d60, d35]);
        race.Checkpoints.AddRange([
            new Checkpoint { Name = "Salida", Kilometer = 0, Order = 1, Type = TimingPointType.Start },
            new Checkpoint { Name = "CP1", Kilometer = 18, Order = 2, Type = TimingPointType.Checkpoint },
            new Checkpoint { Name = "CP2", Kilometer = 39, Order = 3, Type = TimingPointType.Checkpoint },
            new Checkpoint { Name = "Meta", Kilometer = 60, Order = 4, Type = TimingPointType.Finish }
        ]);
        race.Sponsors.AddRange([
            new Sponsor { Name = "Montaña Bike", Order = 1 },
            new Sponsor { Name = "San Luis Deportes", Order = 2 },
            new Sponsor { Name = "NeoMax", Order = 3, WebsiteUrl = "https://neomaxweb.online" }
        ]);
        db.Events.Add(race);
        await db.SaveChangesAsync();

        var cyclists = new[]
        {
            new Cyclist { FirstName = "Martín", LastName = "Rojas", Dni = "30111222", Email = "martin.demo@sprintime.local", Phone = "2664000001", BirthDate = new DateTime(1991, 5, 2), City = "San Luis", Team = "Riders SL" },
            new Cyclist { FirstName = "Lucas", LastName = "Gómez", Dni = "32111222", Email = "lucas.demo@sprintime.local", Phone = "2664000002", BirthDate = new DateTime(1997, 7, 12), City = "Villa Mercedes", Team = "Team Norte" },
            new Cyclist { FirstName = "Diego", LastName = "Pérez", Dni = "29111222", Email = "diego.demo@sprintime.local", Phone = "2664000003", BirthDate = new DateTime(1986, 11, 6), City = "Juana Koslay", Team = "Riders SL" }
        };
        db.Cyclists.AddRange(cyclists);
        await db.SaveChangesAsync();

        var masterA = await db.Categories.FirstAsync(x => x.Name == "Master A");
        var elite = await db.Categories.FirstAsync(x => x.Name == "Elite");
        var regs = new[]
        {
            new Registration { EventId = race.Id, EventDistanceId = d60.Id, CategoryId = masterA.Id, CyclistId = cyclists[0].Id, BibNumber = "024", Status = RegistrationStatus.Confirmed, PaymentStatus = PaymentStatus.Approved, AmountDue = d60.Price, AmountPaid = d60.Price, PaidAt = DateTime.UtcNow.AddDays(-7) },
            new Registration { EventId = race.Id, EventDistanceId = d60.Id, CategoryId = elite.Id, CyclistId = cyclists[1].Id, BibNumber = "117", Status = RegistrationStatus.Confirmed, PaymentStatus = PaymentStatus.Approved, AmountDue = d60.Price, AmountPaid = d60.Price, PaidAt = DateTime.UtcNow.AddDays(-6) },
            new Registration { EventId = race.Id, EventDistanceId = d60.Id, CategoryId = masterA.Id, CyclistId = cyclists[2].Id, BibNumber = "053", Status = RegistrationStatus.Confirmed, PaymentStatus = PaymentStatus.Approved, AmountDue = d60.Price, AmountPaid = d60.Price, PaidAt = DateTime.UtcNow.AddDays(-5) }
        };
        db.Registrations.AddRange(regs);
        await db.SaveChangesAsync();

        var pastDate = DateTime.Today.AddDays(-45).AddHours(9);
        var pastRace = new Event
        {
            Name = "Rally Bike La Florida 2026", Slug = "rally-bike-la-florida-2026", City = "La Florida", Province = "San Luis", Venue = "Dique La Florida",
            StartDate = pastDate, RegistrationOpenAt = pastDate.AddDays(-35), RegistrationCloseAt = pastDate.AddDays(-2).Date.AddHours(22),
            Description = "Circuito rápido y técnico junto al dique, con resultados integrados al historial de temporada.", HeroImageUrl = "/images/hero-race.svg",
            ElevationGainMeters = 780, Status = EventStatus.Finished, ShowPublicParticipants = true
        };
        var d45 = new EventDistance { Name = "MTB 45K", Kilometers = 45, Price = 22000, MaxParticipants = 350 };
        var pastMaster = new Category { Name = "Master A", MinAge = 30, MaxAge = 39, Gender = "Mixto" };
        var pastElite = new Category { Name = "Elite", MinAge = 18, MaxAge = 29, Gender = "Mixto" };
        d45.Categories.AddRange([pastMaster, pastElite]);
        pastRace.Distances.Add(d45);
        db.Events.Add(pastRace);
        await db.SaveChangesAsync();

        var pastRegs = new[]
        {
            new Registration { EventId = pastRace.Id, EventDistanceId = d45.Id, CategoryId = pastMaster.Id, CyclistId = cyclists[0].Id, BibNumber = "024", Status = RegistrationStatus.Confirmed, PaymentStatus = PaymentStatus.Approved, CheckInStatus = CheckInStatus.Finished, AmountDue = d45.Price, AmountPaid = d45.Price, PaidAt = pastDate.AddDays(-10) },
            new Registration { EventId = pastRace.Id, EventDistanceId = d45.Id, CategoryId = pastElite.Id, CyclistId = cyclists[1].Id, BibNumber = "117", Status = RegistrationStatus.Confirmed, PaymentStatus = PaymentStatus.Approved, CheckInStatus = CheckInStatus.Finished, AmountDue = d45.Price, AmountPaid = d45.Price, PaidAt = pastDate.AddDays(-9) },
            new Registration { EventId = pastRace.Id, EventDistanceId = d45.Id, CategoryId = pastMaster.Id, CyclistId = cyclists[2].Id, BibNumber = "053", Status = RegistrationStatus.Confirmed, PaymentStatus = PaymentStatus.Approved, CheckInStatus = CheckInStatus.Finished, AmountDue = d45.Price, AmountPaid = d45.Price, PaidAt = pastDate.AddDays(-8) }
        };
        db.Registrations.AddRange(pastRegs);
        db.Bicycles.Add(new Bicycle { CyclistId = cyclists[0].Id, Name = "MTB Scott", Brand = "Scott", Model = "Scale", Type = "MTB", CurrentKilometers = 1842, KilometersSinceService = 342, ServiceEveryKm = 500 });
        await db.SaveChangesAsync();

        db.Results.AddRange(
            new Result { RegistrationId = pastRegs[0].Id, TotalTime = new TimeSpan(2, 14, 42), GeneralPosition = 3, CategoryPosition = 1, Status = ResultStatus.Official, PublishedAt = pastDate.AddHours(5) },
            new Result { RegistrationId = pastRegs[1].Id, TotalTime = new TimeSpan(2, 12, 8), GeneralPosition = 2, CategoryPosition = 1, Status = ResultStatus.Official, PublishedAt = pastDate.AddHours(5) },
            new Result { RegistrationId = pastRegs[2].Id, TotalTime = new TimeSpan(2, 16, 3), GeneralPosition = 4, CategoryPosition = 2, Status = ResultStatus.Official, PublishedAt = pastDate.AddHours(5) }
        );
        await db.SaveChangesAsync();
    }
}

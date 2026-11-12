using lucho_sprintime.Data;
using lucho_sprintime.Models;
using lucho_sprintime.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

[Authorize(Roles = "Administrator,Organizer,CheckIn,Timer")]
public class RaceDayController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int eventId)
    {
        var race = await db.Events.AsNoTracking().Include(x => x.Checkpoints).FirstOrDefaultAsync(x => x.Id == eventId);
        if (race is null) return NotFound();
        var regs = db.Registrations.Where(x => x.EventId == eventId && x.PaymentStatus == PaymentStatus.Approved);
        return View(new RaceDayViewModel
        {
            Event = race,
            Confirmed = await regs.CountAsync(),
            Accredited = await regs.CountAsync(x => x.CheckInStatus != CheckInStatus.Pending),
            Started = await regs.CountAsync(x => x.CheckInStatus == CheckInStatus.Started || x.CheckInStatus == CheckInStatus.Finished),
            Finished = await regs.CountAsync(x => x.CheckInStatus == CheckInStatus.Finished),
            Checkpoints = race.Checkpoints.OrderBy(x => x.Order).ToList(),
            Recent = await regs.Include(x => x.Cyclist).Include(x => x.Category).OrderByDescending(x => x.CheckedInAt).Take(8).ToListAsync()
        });
    }

    public async Task<IActionResult> CheckIn(int eventId, string? q = null)
    {
        var race = await db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.Id == eventId);
        if (race is null) return NotFound();
        ViewBag.Event = race; ViewBag.Query = q;
        if (string.IsNullOrWhiteSpace(q)) return View(new List<Registration>());
        q = q.Trim();
        var matches = await db.Registrations.AsNoTracking().Where(x => x.EventId == eventId)
            .Include(x => x.Cyclist).Include(x => x.Category).Include(x => x.EventDistance)
            .Where(x => x.Cyclist.Dni == q || (x.BibNumber ?? "") == q || x.Cyclist.FirstName.Contains(q) || x.Cyclist.LastName.Contains(q) || x.QrToken.ToString() == q)
            .Take(12).ToListAsync();
        return View(matches);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCheckIn(int registrationId)
    {
        var reg = await db.Registrations.FirstOrDefaultAsync(x => x.Id == registrationId);
        if (reg is null) return NotFound();
        if (reg.PaymentStatus != PaymentStatus.Approved) { TempData["Error"] = "No se puede acreditar: el pago no está aprobado."; return RedirectToAction(nameof(CheckIn), new { eventId = reg.EventId, q = reg.BibNumber }); }
        reg.CheckInStatus = CheckInStatus.Accredited; reg.CheckedInAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); TempData["Success"] = "Corredor acreditado.";
        return RedirectToAction(nameof(CheckIn), new { eventId = reg.EventId, q = reg.BibNumber });
    }

    public async Task<IActionResult> Timing(int eventId, int checkpointId)
    {
        var race = await db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.Id == eventId);
        var cp = await db.Checkpoints.AsNoTracking().FirstOrDefaultAsync(x => x.Id == checkpointId && x.EventId == eventId);
        if (race is null || cp is null) return NotFound();
        ViewBag.Event = race; ViewBag.Checkpoint = cp;
        return View(await db.TimingRecords.AsNoTracking().Where(x => x.CheckpointId == checkpointId).Include(x => x.Registration).ThenInclude(x => x.Cyclist).OrderByDescending(x => x.TimestampUtc).Take(15).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> RegisterPass([FromBody] TimingEntryInput input)
    {
        var result = await SaveTiming(input);
        return result.ok ? Ok(new { ok = true, name = result.name, timestamp = result.timestamp }) : BadRequest(new { ok = false, error = result.error });
    }

    [HttpPost]
    public async Task<IActionResult> Sync([FromBody] List<TimingEntryInput> entries)
    {
        var synced = 0; var errors = new List<string>();
        foreach (var entry in entries.Take(500))
        {
            var result = await SaveTiming(entry);
            if (result.ok) synced++; else if (!string.IsNullOrWhiteSpace(result.error)) errors.Add($"{entry.BibNumber}: {result.error}");
        }
        return Ok(new { synced, errors });
    }

    private async Task<(bool ok, string? name, DateTime? timestamp, string? error)> SaveTiming(TimingEntryInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.ClientEntryId) && await db.TimingRecords.AnyAsync(x => x.ClientEntryId == input.ClientEntryId))
            return (true, null, input.TimestampUtc, null);
        var cp = await db.Checkpoints.FirstOrDefaultAsync(x => x.Id == input.CheckpointId && x.EventId == input.EventId);
        if (cp is null) return (false, null, null, "Checkpoint inválido.");
        var reg = await db.Registrations.Include(x => x.Cyclist).FirstOrDefaultAsync(x => x.EventId == input.EventId && x.BibNumber == input.BibNumber && x.PaymentStatus == PaymentStatus.Approved);
        if (reg is null) return (false, null, null, "Dorsal no encontrado o pago no confirmado.");
        var timestamp = input.TimestampUtc == default ? DateTime.UtcNow : input.TimestampUtc.ToUniversalTime();
        db.TimingRecords.Add(new TimingRecord { RegistrationId = reg.Id, CheckpointId = cp.Id, TimestampUtc = timestamp, ClientEntryId = string.IsNullOrWhiteSpace(input.ClientEntryId) ? Guid.NewGuid().ToString("N") : input.ClientEntryId, Source = "RaceDay" });
        if (cp.Type == TimingPointType.Start) reg.CheckInStatus = CheckInStatus.Started;
        if (cp.Type == TimingPointType.Finish) reg.CheckInStatus = CheckInStatus.Finished;
        await db.SaveChangesAsync();
        if (cp.Type == TimingPointType.Finish) await RecalculateResults(input.EventId);
        return (true, $"{reg.Cyclist.FirstName} {reg.Cyclist.LastName}", timestamp, null);
    }

    private async Task RecalculateResults(int eventId)
    {
        var start = await db.Checkpoints.AsNoTracking().Where(x => x.EventId == eventId && x.Type == TimingPointType.Start).OrderBy(x => x.Order).FirstOrDefaultAsync();
        var finish = await db.Checkpoints.AsNoTracking().Where(x => x.EventId == eventId && x.Type == TimingPointType.Finish).OrderByDescending(x => x.Order).FirstOrDefaultAsync();
        if (start is null || finish is null) return;
        var regs = await db.Registrations.Where(x => x.EventId == eventId).Include(x => x.TimingRecords).Include(x => x.Result).ToListAsync();
        foreach (var reg in regs)
        {
            var st = reg.TimingRecords.Where(x => x.CheckpointId == start.Id).OrderBy(x => x.TimestampUtc).FirstOrDefault();
            var fn = reg.TimingRecords.Where(x => x.CheckpointId == finish.Id).OrderBy(x => x.TimestampUtc).FirstOrDefault();
            if (st is null || fn is null || fn.TimestampUtc <= st.TimestampUtc) continue;
            reg.Result ??= new Result { RegistrationId = reg.Id };
            reg.Result.TotalTime = fn.TimestampUtc - st.TimestampUtc; reg.Result.Status = ResultStatus.Provisional;
        }
        await db.SaveChangesAsync();

        var completed = await db.Results.Include(x => x.Registration).Where(x => x.Registration.EventId == eventId && x.TotalTime != null).OrderBy(x => x.TotalTime).ToListAsync();
        for (var i = 0; i < completed.Count; i++) completed[i].GeneralPosition = i + 1;
        foreach (var group in completed.GroupBy(x => x.Registration.CategoryId))
        {
            var pos = 1; foreach (var result in group.OrderBy(x => x.TotalTime)) result.CategoryPosition = pos++;
        }
        await db.SaveChangesAsync();
    }
}

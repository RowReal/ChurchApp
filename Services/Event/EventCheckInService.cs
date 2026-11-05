using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Security.Cryptography;
using ChurchApp.Data;
using ChurchApp.Models.Event;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services.Event;

public sealed class EventCheckInService
{
    private readonly AppDbContext db;
    private readonly ChurchApp.Services.AuthService auth;
    private readonly ChurchApp.Services.PrivilegeService privileges;
    private readonly ChurchApp.Services.AuditService audit;
    public EventCheckInService(AppDbContext db, ChurchApp.Services.AuthService auth, ChurchApp.Services.PrivilegeService privileges, ChurchApp.Services.AuditService audit)
    { this.db = db; this.auth = auth; this.privileges = privileges; this.audit = audit; }
    private string WorkerId => auth.CurrentWorker?.WorkerId ?? throw new UnauthorizedAccessException("Please log in.");

    public async Task<List<ChurchEvent>> OpenEventsAsync()
    {
        var id = WorkerId; var now = DateTime.UtcNow;
        return await (from e in db.ChurchEvents.AsNoTracking()
                      join r in db.EventRegistrations.AsNoTracking() on e.Id equals r.ChurchEventId
                      where r.WorkerId == id && r.WillAttend && e.IsPublished && !e.IsCancelled &&
                            e.CheckInOpensAtUtc != null && e.CheckInClosesAtUtc != null &&
                            e.CheckInOpensAtUtc <= now && e.CheckInClosesAtUtc > now
                      orderby e.StartsAtUtc
                      select e).ToListAsync();
    }

    public Task<List<EventMenuOption>> MealsAsync(int eventId) => db.EventMenuOptions.AsNoTracking()
        .Where(m => m.ChurchEventId == eventId && m.IsActive).OrderBy(m => m.Name).ToListAsync();

    private static string NewVerificationToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public async Task<EventCheckIn?> MyCheckInAsync(int eventId)
    {
        var id = WorkerId;
        var record = await db.Set<EventCheckIn>()
            .Include(x => x.EventTable).Include(x => x.EventMenuOption)
            .FirstOrDefaultAsync(x => x.ChurchEventId == eventId && x.WorkerId == id);
        if (record == null) return null;
        // Existing check-ins get a token on their first visit after the migration.
        if (string.IsNullOrEmpty(record.VerificationToken))
        {
            record.VerificationToken = NewVerificationToken();
            await db.SaveChangesAsync();
        }
        return record;
    }

    public async Task<EventCheckIn> CheckInAsync(int eventId, int mealId, double latitude, double longitude, double accuracy)
    {
        var workerId = WorkerId;
        return await AllocateAsync(eventId, mealId, workerId, requireGps: true, latitude, longitude, accuracy);
    }

    public async Task<bool> CanAssistAsync()
    {
        var officer = auth.CurrentWorker;
        return officer != null && officer.IsActive &&
            await privileges.HasPrivilegeAsync(officer.Id, "Access-Event-Management") &&
            await privileges.HasPrivilegeAsync(officer.Id, "Event-Assisted-CheckIn");
    }

    private async Task EnsureAssistingOfficerAsync()
    {
        if (!await CanAssistAsync())
            throw new UnauthorizedAccessException("You are not authorised to perform assisted event check-in.");
    }

    public async Task<List<ChurchEvent>> AssistedEventsAsync()
    {
        await EnsureAssistingOfficerAsync();
        var now = DateTime.UtcNow;
        return await db.ChurchEvents.AsNoTracking()
            .Where(e => e.IsPublished && !e.IsCancelled && e.CheckInOpensAtUtc != null &&
                        e.CheckInClosesAtUtc != null && e.CheckInOpensAtUtc <= now && e.CheckInClosesAtUtc > now)
            .OrderBy(e => e.StartsAtUtc).ToListAsync();
    }

    public async Task<AssistedWorkerResult> FindAssistedWorkerAsync(int eventId, string workerId)
    {
        await EnsureAssistingOfficerAsync();
        workerId = (workerId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(workerId)) throw new InvalidOperationException("Enter a worker ID.");
        var ev = await db.ChurchEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId && e.IsPublished && !e.IsCancelled)
            ?? throw new InvalidOperationException("Event not found.");
        var worker = await db.Workers.AsNoTracking().FirstOrDefaultAsync(w => w.WorkerId == workerId && w.IsActive)
            ?? throw new InvalidOperationException("Active worker not found.");
        if (!await db.EventRegistrations.AsNoTracking().AnyAsync(r => r.ChurchEventId == eventId && r.WorkerId == workerId && r.WillAttend))
            throw new InvalidOperationException("This worker has not registered to attend the selected event.");
        if (EventSeatingEligibility.Classify(worker.Role) == EventSeatingCategory.Excluded)
            throw new InvalidOperationException("This worker's role is not eligible for automatic seating.");
        var existing = await db.EventCheckIns.Include(c => c.EventTable).Include(c => c.EventMenuOption)
            .FirstOrDefaultAsync(c => c.ChurchEventId == eventId && c.WorkerId == workerId);
        if (existing != null && string.IsNullOrEmpty(existing.VerificationToken))
        {
            existing.VerificationToken = NewVerificationToken();
            await db.SaveChangesAsync();
        }
        return new AssistedWorkerResult(worker.WorkerId, worker.FirstName + " " + worker.LastName, existing);
    }

    public async Task<EventCheckIn> AssistedCheckInAsync(int eventId, string workerId, int mealId)
    {
        await EnsureAssistingOfficerAsync();
        var officer = auth.CurrentWorker!;
        workerId = (workerId ?? "").Trim();
        if (string.IsNullOrEmpty(workerId)) throw new InvalidOperationException("Enter a worker ID.");
        // All worker eligibility and registration checks are repeated inside AllocateAsync.
        var existing = await db.EventCheckIns.AsNoTracking().AnyAsync(c => c.ChurchEventId == eventId && c.WorkerId == workerId);
        var result = await AllocateAsync(eventId, mealId, workerId, requireGps: false, 0, 0, 0);
        if (!existing)
        {
            await audit.LogGenericAsync("EventCheckIns", result.Id, "CREATE", officer.Id,
                $"Assisted event check-in: event {eventId}, worker {workerId}, table {result.EventTableId}, seat {result.SeatNumber}; officer {officer.WorkerId}.");
        }
        return result;
    }

    public sealed record AssistedWorkerResult(string WorkerId, string WorkerName, EventCheckIn? ExistingCheckIn);

    private async Task<EventCheckIn> AllocateAsync(int eventId, int mealId, string workerId, bool requireGps,
        double latitude, double longitude, double accuracy)
    {
        // A serializable transaction and database unique indexes are required for safe concurrent allocation.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var now = DateTime.UtcNow;
        var existing = await db.Set<EventCheckIn>().FirstOrDefaultAsync(c => c.ChurchEventId == eventId && c.WorkerId == workerId);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.VerificationToken))
            {
                existing.VerificationToken = NewVerificationToken();
                await db.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            return existing;
        }
        var ev = await db.ChurchEvents.FirstOrDefaultAsync(e => e.Id == eventId && e.IsPublished && !e.IsCancelled)
            ?? throw new InvalidOperationException("Event is unavailable.");
        if (ev.CheckInOpensAtUtc == null || ev.CheckInClosesAtUtc == null || now < ev.CheckInOpensAtUtc || now >= ev.CheckInClosesAtUtc)
            throw new InvalidOperationException("The check-in window is closed.");
        if (!await db.EventRegistrations.AnyAsync(r => r.ChurchEventId == eventId && r.WorkerId == workerId && r.WillAttend))
            throw new InvalidOperationException("You must register to attend this event.");
        var worker = await db.Workers.FirstOrDefaultAsync(w => w.WorkerId == workerId && w.IsActive)
            ?? throw new InvalidOperationException("Active worker profile not found.");
        var category = EventSeatingEligibility.Classify(worker.Role);
        if (category == EventSeatingCategory.Excluded)
            throw new InvalidOperationException("Your role is excluded from automatic event seating.");
        if (!await db.EventMenuOptions.AnyAsync(m => m.Id == mealId && m.ChurchEventId == eventId && m.IsActive))
            throw new InvalidOperationException("Select an available meal.");
        if (ev.MenuChangesCloseAtUtc == null || now >= ev.MenuChangesCloseAtUtc)
            throw new InvalidOperationException("Meal selection has closed. New check-in requires an administrator to resolve the meal choice.");
        if (requireGps) await VerifyLocationAsync(latitude, longitude, accuracy);

        var tables = await db.EventTables.AsNoTracking()
            .Where(t => t.ChurchEventId == eventId && t.IsActive && t.Capacity > 0 &&
                        t.IsLeadershipReserved == (category == EventSeatingCategory.Leadership))
            .OrderBy(t => t.Id).ToListAsync();
        var occupied = await db.Set<EventCheckIn>().AsNoTracking()
            .Where(c => c.ChurchEventId == eventId)
            .Select(c => new { c.EventTableId, c.SeatNumber, c.WorkerId }).ToListAsync();
        var mix = await db.EventSeatingSettings.AsNoTracking()
            .Where(s => s.ChurchEventId == eventId).Select(s => s.MixDirectorates).FirstOrDefaultAsync();
        var assignments = occupied.Where(c => tables.Any(t => t.Id == c.EventTableId)).ToList();
        // Leadership: never use directorate. General: prefer mixing or grouping directorates.
        if (category == EventSeatingCategory.General && worker.DirectorateId.HasValue)
        {
            var seatedWorkerIds = assignments.Select(a => a.WorkerId).Distinct().ToList();
            var directorates = await db.Workers.AsNoTracking().Where(w => seatedWorkerIds.Contains(w.WorkerId))
                .Select(w => new { w.WorkerId, w.DirectorateId }).ToListAsync();
            var counts = assignments.GroupBy(a => a.EventTableId).ToDictionary(g => g.Key,
                g => g.Count(a => directorates.Any(w => w.WorkerId == a.WorkerId && w.DirectorateId == worker.DirectorateId)));
            tables = mix
                ? tables.OrderBy(t => counts.GetValueOrDefault(t.Id)).ThenBy(t => assignments.Count(a => a.EventTableId == t.Id)).ToList()
                : tables.OrderByDescending(t => counts.GetValueOrDefault(t.Id) > 0).ThenByDescending(t => counts.GetValueOrDefault(t.Id)).ThenBy(t => assignments.Count(a => a.EventTableId == t.Id)).ToList();
        }
        foreach (var table in tables)
        {
            var taken = occupied.Where(c => c.EventTableId == table.Id).Select(c => c.SeatNumber).ToHashSet();
            for (int seat = 1; seat <= table.Capacity; seat++)
            {
                if (taken.Contains(seat)) continue;
                var record = new EventCheckIn
                {
                    ChurchEventId = eventId,
                    WorkerId = workerId,
                    EventTableId = table.Id,
                    SeatNumber = seat,
                    EventMenuOptionId = mealId,
                    CheckedInAtUtc = now,
                    VerificationToken = NewVerificationToken(),
                    IsLeadershipSeat = category == EventSeatingCategory.Leadership
                };
                db.Set<EventCheckIn>().Add(record);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return record;
            }
        }
        throw new InvalidOperationException(category == EventSeatingCategory.Leadership
            ? "All leadership-reserved seats are occupied. Please contact the event administrator."
            : "All general seats are occupied. Please contact the event administrator.");
    }

    public async Task ChangeMealAsync(int eventId, int mealId)
    {
        var id = WorkerId;
        var ev = await db.ChurchEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new InvalidOperationException("Event not found.");
        if (ev.MenuChangesCloseAtUtc == null || DateTime.UtcNow >= ev.MenuChangesCloseAtUtc)
            throw new InvalidOperationException("Meal changes have closed.");
        if (!await db.EventMenuOptions.AnyAsync(m => m.Id == mealId && m.ChurchEventId == eventId && m.IsActive))
            throw new InvalidOperationException("Meal is not available.");
        var record = await db.Set<EventCheckIn>().FirstOrDefaultAsync(c => c.ChurchEventId == eventId && c.WorkerId == id)
            ?? throw new InvalidOperationException("Check in first.");
        record.EventMenuOptionId = mealId; record.MenuUpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task VerifyLocationAsync(double latitude, double longitude, double accuracy)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude) || !double.IsFinite(accuracy) ||
            latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180 || accuracy < 0)
            throw new InvalidOperationException("Invalid GPS coordinates.");
        var site = await db.WorkerAttendanceSettings.AsNoTracking().FirstOrDefaultAsync(s => s.IsActive)
            ?? throw new InvalidOperationException("No active church attendance location is configured.");
        if (accuracy > site.MaximumGpsAccuracyMetres)
            throw new InvalidOperationException($"GPS accuracy is insufficient ({accuracy:0}m). Try again outdoors.");
        const double earthRadius = 6371000;
        double ToRad(double degrees) => degrees * Math.PI / 180;
        var dLat = ToRad(site.Latitude - latitude); var dLon = ToRad(site.Longitude - longitude);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(ToRad(latitude)) * Math.Cos(ToRad(site.Latitude)) * Math.Pow(Math.Sin(dLon / 2), 2);
        var metres = earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        if (metres > site.AllowedRadiusMetres)
            throw new InvalidOperationException($"You are approximately {metres:0}m from the approved check-in location (limit {site.AllowedRadiusMetres:0}m).");
    }
}

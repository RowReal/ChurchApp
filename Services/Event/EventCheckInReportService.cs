using ChurchApp.Data;
using ChurchApp.Services;
using ChurchApp.Models.Event;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services.Event;

public sealed class EventCheckInReportRow
{
    public string WorkerId { get; set; } = "";
    public string WorkerName { get; set; } = "";
    public string TableName { get; set; } = "";
    public int SeatNumber { get; set; }
    public string MealName { get; set; } = "";
    public DateTime CheckedInAtUtc { get; set; }
}

public sealed class EventCheckInReportService
{
    private readonly AppDbContext db;
    private readonly AuthService auth;
    private readonly PrivilegeService privileges;

    public EventCheckInReportService(AppDbContext db, AuthService auth, PrivilegeService privileges)
    {
        this.db = db;
        this.auth = auth;
        this.privileges = privileges;
    }

    public async Task<bool> CanViewAsync()
    {
        var worker = auth.CurrentWorker;
        if (!auth.IsAuthenticated || worker == null) return false;
        if (auth.IsInRole(AuthService.Roles.ChurchAdmin, AuthService.Roles.PastorInCharge)) return true;
        return await privileges.HasPrivilegeAsync(worker.Id, "Access-Event-Management");
    }

    private async Task RequireAccessAsync()
    {
        if (!await CanViewAsync())
            throw new UnauthorizedAccessException("You do not have access to the Event Check-In Report.");
    }

    public async Task<List<ChurchEvent>> GetEventsAsync()
    {
        await RequireAccessAsync();
        return await db.ChurchEvents.AsNoTracking()
            .OrderByDescending(e => e.StartsAtUtc).ToListAsync();
    }

    public async Task<List<EventCheckInReportRow>> GetRowsAsync(int eventId)
    {
        await RequireAccessAsync();
        if (!await db.ChurchEvents.AsNoTracking().AnyAsync(e => e.Id == eventId))
            throw new InvalidOperationException("Event not found.");

        var checkIns = await db.EventCheckIns.AsNoTracking()
            .Where(c => c.ChurchEventId == eventId)
            .Include(c => c.EventTable)
            .Include(c => c.EventMenuOption)
            .OrderBy(c => c.EventTable.TableName)
            .ThenBy(c => c.SeatNumber)
            .ToListAsync();

        var ids = checkIns.Select(c => c.WorkerId).Distinct().ToList();
        var workers = await db.Workers.AsNoTracking()
            .Where(w => ids.Contains(w.WorkerId))
            .Select(w => new { w.WorkerId, w.FirstName, w.LastName })
            .ToListAsync();
        var names = workers.GroupBy(w => w.WorkerId)
            .ToDictionary(g => g.Key, g => (g.First().FirstName + " " + g.First().LastName).Trim());

        return checkIns.Select(c => new EventCheckInReportRow
        {
            WorkerId = c.WorkerId,
            WorkerName = names.TryGetValue(c.WorkerId, out var name) ? name : c.WorkerId,
            TableName = c.EventTable?.TableName ?? "—",
            SeatNumber = c.SeatNumber,
            MealName = c.EventMenuOption?.Name ?? "—",
            CheckedInAtUtc = c.CheckedInAtUtc
        }).ToList();
    }
}



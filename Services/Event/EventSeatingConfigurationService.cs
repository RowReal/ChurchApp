using ChurchApp.Data;
using ChurchApp.Models.Event;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services.Event;

public class EventSeatingConfigurationService
{
    private readonly AppDbContext db;
    private readonly AuthService auth;
    public EventSeatingConfigurationService(AppDbContext db, AuthService auth)
    {
        this.db = db;
        this.auth = auth;
    }

    private void RequireAdmin()
    {
        if (!auth.IsInRole(AuthService.Roles.ChurchAdmin, AuthService.Roles.PastorInCharge))
            throw new UnauthorizedAccessException("Only Church Admin or Pastor in Charge can configure event seating.");
    }

    public async Task<List<ChurchEvent>> GetEventsAsync()
    {
        RequireAdmin();
        return await db.ChurchEvents.AsNoTracking().OrderByDescending(e => e.StartsAtUtc).ToListAsync();
    }

    public async Task<List<EventTable>> GetTablesAsync(int eventId)
    {
        RequireAdmin();
        return await db.EventTables.AsNoTracking().Where(t => t.ChurchEventId == eventId)
            .OrderBy(t => t.Id).ToListAsync();
    }

    public async Task<bool> GetMixDirectoratesAsync(int eventId)
    {
        RequireAdmin();
        return (await db.EventSeatingSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.ChurchEventId == eventId))?.MixDirectorates ?? true;
    }

    public async Task SaveMixDirectoratesAsync(int eventId, bool mix)
    {
        RequireAdmin();
        if (!await db.ChurchEvents.AnyAsync(e => e.Id == eventId))
            throw new InvalidOperationException("Event not found.");
        var settings = await db.EventSeatingSettings.FirstOrDefaultAsync(s => s.ChurchEventId == eventId);
        if (settings == null)
        {
            settings = new EventSeatingSettings { ChurchEventId = eventId, MixDirectorates = mix };
            db.EventSeatingSettings.Add(settings);
        }
        else settings.MixDirectorates = mix;
        await db.SaveChangesAsync();
    }

    public async Task SaveTableAsync(int eventId, int? tableId, string name, int capacity, bool leadership, bool active)
    {
        RequireAdmin();
        name = (name ?? "").Trim();
        if (name.Length == 0 || name.Length > 80) throw new InvalidOperationException("Table name must be 1–80 characters.");
        if (capacity < 1 || capacity > 100) throw new InvalidOperationException("Capacity must be between 1 and 100.");
        if (!await db.ChurchEvents.AnyAsync(e => e.Id == eventId)) throw new InvalidOperationException("Event not found.");
        if (await db.EventTables.AnyAsync(t => t.ChurchEventId == eventId && t.TableName == name && (!tableId.HasValue || t.Id != tableId.Value)))
            throw new InvalidOperationException("A table with this name already exists for this event.");
        EventTable table;
        if (tableId.HasValue)
            table = await db.EventTables.FirstOrDefaultAsync(t => t.Id == tableId.Value && t.ChurchEventId == eventId)
                ?? throw new InvalidOperationException("Table not found for this event.");
        else
        {
            table = new EventTable { ChurchEventId = eventId };
            db.EventTables.Add(table);
        }
        table.TableName = name;
        table.Capacity = capacity;
        table.IsLeadershipReserved = leadership;
        table.IsActive = active;
        await db.SaveChangesAsync();
    }
}

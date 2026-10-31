using ChurchApp.Data;
using ChurchApp.Models.Event;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services.Event;

public class EventMenuService
{
    private readonly AppDbContext _context;

    public EventMenuService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<EventMenuOption>> GetMenuAsync(int eventId)
    {
        return await _context.Set<EventMenuOption>()
            .AsNoTracking()
            .Where(m => m.ChurchEventId == eventId)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task SaveMenuAsync(int eventId, int? menuId, string name, bool isActive)
    {
        name = (name ?? string.Empty).Trim();
        if (eventId <= 0 || !await _context.Set<ChurchEvent>().AnyAsync(e => e.Id == eventId))
            throw new InvalidOperationException("Please select a valid event.");
        if (name.Length == 0 || name.Length > 120)
            throw new InvalidOperationException("Enter a meal name (up to 120 characters).");

        var duplicate = await _context.Set<EventMenuOption>().AnyAsync(m =>
            m.ChurchEventId == eventId && m.Id != menuId && m.Name.ToLower() == name.ToLower());
        if (duplicate)
            throw new InvalidOperationException("This meal already exists for the selected event.");

        if (menuId.HasValue)
        {
            var existing = await _context.Set<EventMenuOption>().FirstOrDefaultAsync(m =>
                m.Id == menuId.Value && m.ChurchEventId == eventId);
            if (existing == null)
                throw new InvalidOperationException("The selected meal was not found.");
            existing.Name = name;
            existing.IsActive = isActive;
        }
        else
        {
            _context.Set<EventMenuOption>().Add(new EventMenuOption
            {
                ChurchEventId = eventId,
                Name = name,
                IsActive = isActive
            });
        }

        await _context.SaveChangesAsync();
    }
}


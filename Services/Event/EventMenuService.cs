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
        var meals = await _context.Set<EventMenuOption>()
            .AsNoTracking()
            .Where(m => m.ChurchEventId == eventId)
            .OrderBy(m => m.Name)
            .ToListAsync();
        foreach (var meal in meals)
            meal.AllocatedQuantity = await CountAllocatedAsync(_context, eventId, meal.Id);
        return meals;
    }

    internal static async Task<int> CountAllocatedAsync(AppDbContext db, int eventId, int mealId)
    {
        return await db.EventCheckIns.CountAsync(c => c.ChurchEventId == eventId && c.EventMenuOptionId == mealId)
            + await db.EventSeatReservations.CountAsync(r => r.ChurchEventId == eventId && r.IsGuest && r.ReleasedAtUtc == null && r.GuestMealOptionId == mealId);
    }

    public async Task SaveMenuAsync(int eventId, int? menuId, string name, bool isActive, int? quantity = null)
    {
        name = (name ?? string.Empty).Trim();
        if (eventId <= 0 || !await _context.Set<ChurchEvent>().AnyAsync(e => e.Id == eventId))
            throw new InvalidOperationException("Please select a valid event.");
        if (name.Length == 0 || name.Length > 120)
            throw new InvalidOperationException("Enter a meal name (up to 120 characters).");

        if (quantity < 0) throw new InvalidOperationException("Quantity cannot be negative.");
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (menuId.HasValue && quantity.HasValue && quantity.Value < await CountAllocatedAsync(_context, eventId, menuId.Value))
            throw new InvalidOperationException("Quantity cannot be lower than the portions already allocated.");
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
            existing.Quantity = quantity;
        }
        else
        {
            _context.Set<EventMenuOption>().Add(new EventMenuOption
            {
                ChurchEventId = eventId,
                Name = name,
                IsActive = isActive,
                Quantity = quantity
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}


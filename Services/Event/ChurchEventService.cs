using ChurchApp.Data;
using ChurchApp.Models.Event;
using ChurchApp.Services;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services.Event;

public class ChurchEventService
{
    private readonly AppDbContext _context;
    private readonly AuthService _auth;

    public ChurchEventService(AppDbContext context, AuthService auth)
    {
        _context = context;
        _auth = auth;
    }

    private void EnsureAdmin()
    {
        if (!_auth.IsAuthenticated || !_auth.CanAccessAdminPanel())
            throw new UnauthorizedAccessException("Only authorised administrators can manage events.");
    }

    public async Task<List<ChurchEvent>> GetAllAsync()
    {
        EnsureAdmin();
        return await _context.ChurchEvents.AsNoTracking()
            .OrderByDescending(x => x.StartsAtUtc).ToListAsync();
    }

    public async Task SaveAsync(ChurchEvent input)
    {
        EnsureAdmin();
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Venue))
            throw new InvalidOperationException("Event name and venue are required.");
        if (input.EndsAtUtc <= input.StartsAtUtc)
            throw new InvalidOperationException("Event end must be after its start.");
        if (input.RegistrationClosesAtUtc <= input.RegistrationOpensAtUtc)
            throw new InvalidOperationException("Registration closing time must be after opening time.");
        if (input.RegistrationClosesAtUtc > input.StartsAtUtc)
            throw new InvalidOperationException("Registration must close before the event starts.");

        if (!input.CheckInOpensAtUtc.HasValue || !input.CheckInClosesAtUtc.HasValue || !input.MenuChangesCloseAtUtc.HasValue)
            throw new InvalidOperationException("Check-in opening, check-in closing and menu change closing times are required.");
        if (input.CheckInClosesAtUtc <= input.CheckInOpensAtUtc)
            throw new InvalidOperationException("Check-in closing time must be after check-in opening time.");
        if (input.MenuChangesCloseAtUtc < input.CheckInOpensAtUtc || input.MenuChangesCloseAtUtc > input.CheckInClosesAtUtc)
            throw new InvalidOperationException("Menu change deadline must fall within the check-in period.");
        if (input.CheckInOpensAtUtc > input.EndsAtUtc || input.CheckInClosesAtUtc > input.EndsAtUtc)
            throw new InvalidOperationException("Check-in must close no later than the event end.");

        ChurchEvent entity;
        if (input.Id == 0)
        {
            entity = new ChurchEvent { CreatedAtUtc = DateTime.UtcNow };
            _context.ChurchEvents.Add(entity);
        }
        else
        {
            entity = await _context.ChurchEvents.FirstOrDefaultAsync(x => x.Id == input.Id)
                ?? throw new InvalidOperationException("Event no longer exists.");
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        entity.Name = input.Name.Trim();
        entity.Description = input.Description?.Trim();
        entity.Venue = input.Venue.Trim();
        entity.StartsAtUtc = input.StartsAtUtc;
        entity.EndsAtUtc = input.EndsAtUtc;
        entity.RegistrationOpensAtUtc = input.RegistrationOpensAtUtc;
        entity.RegistrationClosesAtUtc = input.RegistrationClosesAtUtc;
        entity.CheckInOpensAtUtc = input.CheckInOpensAtUtc;
        entity.CheckInClosesAtUtc = input.CheckInClosesAtUtc;
        entity.MenuChangesCloseAtUtc = input.MenuChangesCloseAtUtc;
        entity.IsPublished = input.IsPublished;
        entity.IsCancelled = input.IsCancelled;
        await _context.SaveChangesAsync();
    }
}


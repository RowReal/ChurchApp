using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class PrayerFocusService
    {
        private readonly AppDbContext _context;

        public PrayerFocusService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PrayerFocus?> GetCurrentPrayerFocusAsync()
        {
            var today = DateTime.Today;

            return await _context.PrayerFocuses
                .Include(p => p.PrayerPoints.OrderBy(x => x.DisplayOrder))
                .Where(p => p.IsActive &&
                            p.WeekStartDate.Date <= today &&
                            p.WeekEndDate.Date >= today)
                .OrderByDescending(p => p.CreatedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<PrayerFocus>> GetAllPrayerFocusesAsync()
        {
            return await _context.PrayerFocuses
                .Include(p => p.PrayerPoints.OrderBy(x => x.DisplayOrder))
                .OrderByDescending(p => p.WeekStartDate)
                .ToListAsync();
        }

        public async Task<PrayerFocus?> GetPrayerFocusByIdAsync(int id)
        {
            return await _context.PrayerFocuses
                .Include(p => p.PrayerPoints.OrderBy(x => x.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task CreatePrayerFocusAsync(PrayerFocus prayerFocus)
        {
            prayerFocus.CreatedDate = DateTime.UtcNow;

            prayerFocus.PrayerPoints = prayerFocus.PrayerPoints
                .Where(p => !string.IsNullOrWhiteSpace(p.PointText))
                .Select((p, index) =>
                {
                    p.DisplayOrder = index + 1;
                    return p;
                })
                .ToList();

            _context.PrayerFocuses.Add(prayerFocus);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePrayerFocusAsync(PrayerFocus prayerFocus)
        {
            var existing = await _context.PrayerFocuses
                .Include(p => p.PrayerPoints)
                .FirstOrDefaultAsync(p => p.Id == prayerFocus.Id);

            if (existing == null)
            {
                return;
            }

            existing.Theme = prayerFocus.Theme;
            existing.BibleVerse = prayerFocus.BibleVerse;
            existing.WeekStartDate = prayerFocus.WeekStartDate;
            existing.WeekEndDate = prayerFocus.WeekEndDate;
            existing.IsActive = prayerFocus.IsActive;

            _context.PrayerPoints.RemoveRange(existing.PrayerPoints);

            existing.PrayerPoints = prayerFocus.PrayerPoints
                .Where(p => !string.IsNullOrWhiteSpace(p.PointText))
                .Select((p, index) => new PrayerPoint
                {
                    PointText = p.PointText,
                    DisplayOrder = index + 1,
                    PrayerFocusId = existing.Id
                })
                .ToList();

            await _context.SaveChangesAsync();
        }

        public async Task ToggleActiveStatusAsync(int id)
        {
            var prayerFocus = await _context.PrayerFocuses.FindAsync(id);

            if (prayerFocus == null)
            {
                return;
            }

            prayerFocus.IsActive = !prayerFocus.IsActive;
            await _context.SaveChangesAsync();
        }

        public async Task DeletePrayerFocusAsync(int id)
        {
            var prayerFocus = await _context.PrayerFocuses
                .Include(p => p.PrayerPoints)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prayerFocus == null)
            {
                return;
            }

            _context.PrayerPoints.RemoveRange(prayerFocus.PrayerPoints);
            _context.PrayerFocuses.Remove(prayerFocus);

            await _context.SaveChangesAsync();
        }
    }
}
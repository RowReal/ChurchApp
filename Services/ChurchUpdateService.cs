using ChurchApp.Data;
using ChurchApp.Models;
using ChurchApp.Models;
using ChurchApp.Data;
using Microsoft.EntityFrameworkCore;
namespace ChurchApp.Services
{
    public class ChurchUpdateService
    {
        private readonly AppDbContext _context;

        public ChurchUpdateService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ChurchAnnouncement?> GetLatestAnnouncementAsync()
        {
            return await _context.ChurchAnnouncements
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.EventDate)
                .FirstOrDefaultAsync();
        }

        public async Task<PrayerFocus?> GetCurrentPrayerFocusAsync()
        {
            return await _context.PrayerFocuses
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.WeekStartDate)
                .FirstOrDefaultAsync();
        }

        public async Task<VerseOfTheDay?> GetTodayVerseAsync()
        {
            var today = DateTime.Today;

            return await _context.VerseOfTheDays
                .Where(v => v.IsActive && v.DisplayDate.Date == today)
                .FirstOrDefaultAsync();
        }
    }
}

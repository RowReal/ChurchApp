using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class VerseOfTheDayService
    {
        private readonly AppDbContext _context;

        public VerseOfTheDayService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<VerseOfTheDay?> GetTodayVerseAsync()
        {
            var today = DateTime.Today;

            return await _context.VerseOfTheDays
                .Where(v => v.IsActive && v.DisplayDate.Date == today)
                .OrderByDescending(v => v.CreatedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<VerseOfTheDay>> GetAllVersesAsync()
        {
            return await _context.VerseOfTheDays
                .OrderByDescending(v => v.DisplayDate)
                .ToListAsync();
        }

        public async Task CreateVerseAsync(VerseOfTheDay verse)
        {
            verse.CreatedDate = DateTime.UtcNow;
            _context.VerseOfTheDays.Add(verse);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVerseAsync(VerseOfTheDay verse)
        {
            var existing = await _context.VerseOfTheDays.FindAsync(verse.Id);

            if (existing == null)
            {
                return;
            }

            existing.VerseText = verse.VerseText;
            existing.ScriptureReference = verse.ScriptureReference;
            existing.DisplayDate = verse.DisplayDate;
            existing.IsActive = verse.IsActive;

            await _context.SaveChangesAsync();
        }

        public async Task ToggleActiveStatusAsync(int id)
        {
            var verse = await _context.VerseOfTheDays.FindAsync(id);

            if (verse == null)
            {
                return;
            }

            verse.IsActive = !verse.IsActive;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVerseAsync(int id)
        {
            var verse = await _context.VerseOfTheDays.FindAsync(id);

            if (verse == null)
            {
                return;
            }

            _context.VerseOfTheDays.Remove(verse);
            await _context.SaveChangesAsync();
        }
    }
}
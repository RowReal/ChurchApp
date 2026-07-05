using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ChurchNoticeService
    {
        private readonly AppDbContext _context;

        public ChurchNoticeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ChurchNotice?> GetDashboardNoticeAsync()
        {
            var now = DateTime.UtcNow;

            return await _context.ChurchNotices
                .Where(n => n.IsActive &&
                            n.PublishDate <= now &&
                            (n.ExpiryDate == null || n.ExpiryDate >= now))
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.PublishDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ChurchNotice>> GetAllNoticesAsync()
        {
            return await _context.ChurchNotices
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.PublishDate)
                .ToListAsync();
        }

        public async Task<ChurchNotice?> GetNoticeByIdAsync(int id)
        {
            return await _context.ChurchNotices
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task CreateNoticeAsync(ChurchNotice notice)
        {
            notice.CreatedDate = DateTime.UtcNow;
            _context.ChurchNotices.Add(notice);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateNoticeAsync(ChurchNotice notice)
        {
            var existing = await _context.ChurchNotices.FindAsync(notice.Id);

            if (existing == null)
            {
                return;
            }

            existing.Title = notice.Title;
            existing.Message = notice.Message;
            existing.Type = notice.Type;
            existing.PublishDate = notice.PublishDate;
            existing.ExpiryDate = notice.ExpiryDate;
            existing.IsPinned = notice.IsPinned;
            existing.IsActive = notice.IsActive;

            await _context.SaveChangesAsync();
        }

        public async Task ToggleActiveStatusAsync(int id)
        {
            var notice = await _context.ChurchNotices.FindAsync(id);

            if (notice == null)
            {
                return;
            }

            notice.IsActive = !notice.IsActive;
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChurchNotice>> GetActiveDashboardNoticesAsync()
        {
            var today = DateTime.Today;

            return await _context.ChurchNotices
                .Where(n => n.IsActive)
                .Where(n => n.PublishDate.Date <= today)
                .Where(n => n.ExpiryDate == null || n.ExpiryDate.Value.Date >= today)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.PublishDate)
                .Take(3)
                .ToListAsync();
        }
        public async Task DeleteNoticeAsync(int id)
        {
            var notice = await _context.ChurchNotices.FindAsync(id);

            if (notice == null)
            {
                return;
            }

            _context.ChurchNotices.Remove(notice);
            await _context.SaveChangesAsync();
        }
    }

}
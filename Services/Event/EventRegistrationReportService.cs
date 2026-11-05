using ChurchApp.Data;
using ChurchApp.Models.Event;
using ChurchApp.Services;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services.Event
{
    public sealed class EventRegistrationReportRow
    {
        public string WorkerId { get; set; } = "";
        public string WorkerName { get; set; } = "";
        public string Directorate { get; set; } = "";
        public bool WillAttend { get; set; }
        public string RegisteredBy { get; set; } = "";
        public DateTime RegisteredAtUtc { get; set; }
    }

    public class EventRegistrationReportService
    {
        private readonly AppDbContext _db;
        private readonly AuthService _auth;
        private readonly PrivilegeService _privilegeService;
        public EventRegistrationReportService(AppDbContext db, AuthService auth, PrivilegeService privilegeService)
        {
            _db = db;
            _auth = auth;
            _privilegeService = privilegeService;
        }

        private async Task RequireAccessAsync()
        {
            var worker = _auth.CurrentWorker;
            if (!_auth.IsAuthenticated || worker == null)
                throw new UnauthorizedAccessException("You must be logged in to view event registration reports.");

            if (_auth.IsInRole(AuthService.Roles.ChurchAdmin, AuthService.Roles.PastorInCharge))
                return;

            var hasPrivilege = await _privilegeService.HasPrivilegeAsync(
                worker.Id, "Access-Event-Management");

            if (!hasPrivilege)
                throw new UnauthorizedAccessException(
                    "Only Church Admin, Pastor in Charge, or workers with Event Management privilege can view event registration reports.");
        }

        public async Task<List<ChurchEvent>> GetEventsAsync()
        {
            await RequireAccessAsync();
            return await _db.ChurchEvents.AsNoTracking()
                .OrderByDescending(e => e.StartsAtUtc).ToListAsync();
        }

        public async Task<List<EventRegistrationReportRow>> GetRegistrationsAsync(int eventId)
        {
            await RequireAccessAsync();
            if (!await _db.ChurchEvents.AnyAsync(e => e.Id == eventId))
                throw new InvalidOperationException("Event not found.");

            var registrations = await _db.EventRegistrations.AsNoTracking()
                .Where(r => r.ChurchEventId == eventId)
                .OrderByDescending(r => r.RegisteredAtUtc)
                .ToListAsync();

            var ids = registrations.Select(r => r.WorkerId)
                .Concat(registrations.Where(r => !string.IsNullOrWhiteSpace(r.AssistedByWorkerId))
                    .Select(r => r.AssistedByWorkerId!))
                .Distinct().ToList();

            var workers = await _db.Workers.AsNoTracking()
                .Include(w => w.Directorate)
                .Where(w => ids.Contains(w.WorkerId)).ToListAsync();
            var lookup = workers.GroupBy(w => w.WorkerId)
                .ToDictionary(g => g.Key, g => g.First());

            string Name(string id) => lookup.TryGetValue(id, out var w)
                ? $"{w.FirstName} {w.LastName}".Trim() : id;

            return registrations.Select(r => new EventRegistrationReportRow
            {
                WorkerId = r.WorkerId,
                WorkerName = Name(r.WorkerId),
                Directorate = lookup.TryGetValue(r.WorkerId, out var w)
                    ? (w.Directorate?.Name ?? "—") : "—",
                WillAttend = r.WillAttend,
                RegisteredBy = string.IsNullOrWhiteSpace(r.AssistedByWorkerId)
                    ? "Self" : Name(r.AssistedByWorkerId),
                RegisteredAtUtc = r.RegisteredAtUtc
            }).ToList();
        }
    }
}


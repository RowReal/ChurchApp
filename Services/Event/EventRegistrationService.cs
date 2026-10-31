using ChurchApp.Data;
using ChurchApp.Models.Event;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services.Event
{
    public sealed class EventWorkerStatus
    {
        public string WorkerId { get; set; } = "";
        public string WorkerName { get; set; } = "";
        public bool IsRegistered { get; set; }
        public bool WillAttend { get; set; }
        public string RegisteredByWorkerId { get; set; } = "";
        public string RegisteredByName { get; set; } = "";
        public bool CanUnregister { get; set; }
    }

    public class EventRegistrationService
    {
        private readonly AppDbContext _context;
        private readonly AuthService _auth;
        public EventRegistrationService(AppDbContext context, AuthService auth)
        { _context = context; _auth = auth; }

        private string CurrentId() => _auth.CurrentWorker?.WorkerId
            ?? throw new UnauthorizedAccessException("Please sign in.");

        public async Task<List<ChurchEvent>> GetAvailableEventsAsync()
        {
            CurrentId();
            var now = DateTime.UtcNow;
            return await _context.ChurchEvents.AsNoTracking()
                .Where(e => e.IsPublished && !e.IsCancelled &&
                    e.RegistrationOpensAtUtc <= now && e.RegistrationClosesAtUtc > now)
                .OrderBy(e => e.StartsAtUtc).ToListAsync();
        }

        public async Task<EventRegistration?> GetMyRegistrationAsync(int eventId)
        {
            var id = CurrentId();
            return await _context.EventRegistrations.AsNoTracking()
                .FirstOrDefaultAsync(r => r.ChurchEventId == eventId && r.WorkerId == id);
        }

        public async Task<EventWorkerStatus> GetWorkerStatusAsync(int eventId, string workerId)
        {
            CurrentId();
            await CheckEventAsync(eventId);
            workerId = (workerId ?? "").Trim();
            if (workerId.Length == 0) throw new InvalidOperationException("Enter a Worker ID.");
            var worker = await _context.Workers.AsNoTracking()
                .FirstOrDefaultAsync(w => w.WorkerId == workerId && w.IsActive)
                ?? throw new InvalidOperationException($"Worker ID {workerId} was not found or is inactive.");
            var registration = await _context.EventRegistrations.AsNoTracking()
                .FirstOrDefaultAsync(r => r.ChurchEventId == eventId && r.WorkerId == workerId);
            var registrarId = registration == null ? "" :
                (string.IsNullOrWhiteSpace(registration.AssistedByWorkerId) ? workerId : registration.AssistedByWorkerId);
            var registrar = registration == null ? null : await _context.Workers.AsNoTracking()
                .FirstOrDefaultAsync(w => w.WorkerId == registrarId);
            return new EventWorkerStatus
            {
                WorkerId = workerId,
                WorkerName = $"{worker.FirstName} {worker.LastName}".Trim(),
                IsRegistered = registration != null,
                WillAttend = registration?.WillAttend ?? false,
                RegisteredByWorkerId = registrarId,
                RegisteredByName = registrar == null ? registrarId : $"{registrar.FirstName} {registrar.LastName}".Trim(),
                CanUnregister = registration != null && (workerId == CurrentId() || registrarId == CurrentId())
            };
        }

        public Task<EventRegistration> RegisterSelfAsync(int eventId, bool willAttend) =>
            RegisterCoreAsync(eventId, CurrentId(), willAttend, false);

        public Task<EventRegistration> RegisterAssistedAsync(int eventId, string workerId, bool willAttend) =>
            RegisterCoreAsync(eventId, (workerId ?? "").Trim(), willAttend, true);

        private async Task<EventRegistration> RegisterCoreAsync(int eventId, string workerId, bool willAttend, bool assisted)
        {
            var actor = CurrentId();
            await CheckEventAsync(eventId);
            if (assisted && workerId == actor)
                throw new InvalidOperationException("Use Self Registration for your own registration.");
            if (string.IsNullOrWhiteSpace(workerId))
                throw new InvalidOperationException("Enter a Worker ID.");
            var exists = await _context.Workers.AnyAsync(w => w.WorkerId == workerId && w.IsActive);
            if (!exists) throw new InvalidOperationException("Active Worker ID not found.");
            if (await _context.EventRegistrations.AnyAsync(r => r.ChurchEventId == eventId && r.WorkerId == workerId))
                throw new InvalidOperationException("This worker is already registered. Duplicate registration is not allowed.");
            var registration = new EventRegistration
            {
                ChurchEventId = eventId,
                WorkerId = workerId,
                WillAttend = willAttend,
                RegisteredAtUtc = DateTime.UtcNow,
                AssistedByWorkerId = assisted ? actor : null
            };
            _context.EventRegistrations.Add(registration);
            await CheckEventAsync(eventId);
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                _context.Entry(registration).State = EntityState.Detached;
                throw new InvalidOperationException("This worker has already been registered. Please refresh.");
            }
            return registration;
        }

        public async Task UnregisterAsync(int eventId, string workerId)
        {
            var actor = CurrentId();
            await CheckEventAsync(eventId);
            var registration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.ChurchEventId == eventId && r.WorkerId == workerId)
                ?? throw new InvalidOperationException("Registration not found.");
            var registrar = string.IsNullOrWhiteSpace(registration.AssistedByWorkerId)
                ? registration.WorkerId : registration.AssistedByWorkerId;
            if (registration.WorkerId != actor && registrar != actor)
                throw new UnauthorizedAccessException("Only the registered worker or the person who registered them can unregister.");
            _context.EventRegistrations.Remove(registration);
            await CheckEventAsync(eventId);
            await _context.SaveChangesAsync();
        }

        private async Task CheckEventAsync(int eventId)
        {
            var ev = await _context.ChurchEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId)
                ?? throw new InvalidOperationException("Event not found.");
            var now = DateTime.UtcNow;
            if (!ev.IsPublished || ev.IsCancelled) throw new InvalidOperationException("Event registration is unavailable.");
            if (now < ev.RegistrationOpensAtUtc) throw new InvalidOperationException("Registration has not opened.");
            if (now >= ev.RegistrationClosesAtUtc) throw new InvalidOperationException("Registration is closed.");
        }
    }
}



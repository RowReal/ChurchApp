using ChurchApp.Data;
using ChurchApp.Models.Bricks;
using Microsoft.EntityFrameworkCore;
namespace ChurchApp.Services.Bricks;
public class BricksPrivilegeService
{
    public const string Centres = "Manage-Bricks-Centres";
    public const string Members = "Manage-Bricks-Members";
    public const string Schedule = "Manage-Bricks-Schedule";
    public const string Attendance = "Manage-Bricks-Attendance";
    public const string Offerings = "Manage-Bricks-Offerings";
    public const string Reports = "View-Bricks-Reports";
    private readonly AppDbContext db;
    public BricksPrivilegeService(AppDbContext db) => this.db = db;
    public static readonly (string Code, string Name, string Description)[] Definitions =
    {
        (Centres,"Manage Fellowship Centres","Create, edit and manage all centres"),
        (Members,"Manage Fellowship Members","Manage members across fellowship centres"),
        (Schedule,"Manage Fellowship Calendar","Manage fellowship schedules and closures"),
        (Attendance,"Manage Attendance","Manage attendance across authorised centres"),
        (Offerings,"Manage Offerings","Manage fellowship offerings across authorised centres"),
        (Reports,"View All Reports","View reports across all fellowship centres")
    };
    // Directorate leadership is read from the registered Directorate, not from a worker's general role.
    private IQueryable<ChurchApp.Models.Directorate> BricksDirectorates() =>
        db.Directorates.AsNoTracking().Where(d => d.IsActive &&
            (d.Code.ToUpper() == "BRICK" || d.Code.ToUpper() == "BRICKS" ||
             d.Name.ToUpper() == "BRICK" || d.Name.ToUpper() == "BRICKS" ||
             d.Name.ToUpper().Contains("BRICK DIRECTORATE") ||
             d.Name.ToUpper().Contains("BRICKS DIRECTORATE")));

    // Accept either the explicitly registered directorate head or a worker whose
    // current role is Head of Directorate and whose assigned directorate is BRICKS.
    // This avoids denying access when HeadWorkerId has not been maintained.
    public async Task<bool> IsBricksHeadAsync(int workerId)
    {
        if (await BricksDirectorates().AnyAsync(d => d.HeadWorkerId == workerId))
            return true;

        return await db.Workers.AsNoTracking().AnyAsync(w =>
            w.Id == workerId && w.IsActive &&
            w.Role == "Head of Directorate" &&
            w.DirectorateId.HasValue &&
            BricksDirectorates().Any(d => d.Id == w.DirectorateId.Value));
    }

    public async Task<bool> IsBricksAssistantAsync(int workerId) =>
        await BricksDirectorates().AnyAsync(d => d.AssistantHeadWorkerId == workerId);

    private async Task<bool> HasAssignedBricksPrivilegeAsync(int workerId, string? code = null) =>
        await db.BricksUserPrivileges.AsNoTracking().AnyAsync(p =>
            p.WorkerId == workerId && p.IsActive && p.Privilege.IsActive &&
            (code == null || p.Privilege.Code == code));

    // Head has automatic BRICKS administration. Assistant becomes an administrator
    // after receiving at least one BRICKS privilege; neither can grant church-wide privileges.
    public async Task<bool> IsAdministratorAsync(int workerId) =>
        await IsBricksHeadAsync(workerId) ||
        (await IsBricksAssistantAsync(workerId) &&
         await HasAssignedBricksPrivilegeAsync(workerId));

    public async Task<bool> HasPrivilegeAsync(int workerId, string code) =>
        await IsAdministratorAsync(workerId) ||
        await HasAssignedBricksPrivilegeAsync(workerId, code);

    public async Task<bool> IsCentreLeaderAsync(int workerId, int? centreId = null) =>
        await db.HouseFellowshipCentres.AsNoTracking().AnyAsync(c =>
            (!centreId.HasValue || c.Id == centreId.Value) &&
            (c.LeaderWorkerId == workerId || c.AssistantWorkerId == workerId));

    // Sidebar/workspace visibility: Pastor in Charge OR BRICKS Head OR any active BRICKS privilege.
    // A centre assignment alone does not make the menu visible.
    public async Task<bool> CanAccessAsync(int workerId)
    {
        if (await db.Workers.AsNoTracking().AnyAsync(w =>
            w.Id == workerId && w.IsActive && w.Role == "Pastor in Charge")) return true;
        return await IsBricksHeadAsync(workerId) ||
               await HasAssignedBricksPrivilegeAsync(workerId);
    }
    public async Task<bool> CanManageCentreMembersAsync(int workerId, int centreId) =>
        await HasPrivilegeAsync(workerId, Members) || await IsCentreLeaderAsync(workerId, centreId);
    public async Task<bool> CanRecordCentreAttendanceAsync(int workerId, int centreId) =>
        await HasPrivilegeAsync(workerId, Attendance) || await IsCentreLeaderAsync(workerId, centreId);
    public async Task<bool> CanRecordCentreOfferingAsync(int workerId, int centreId) =>
        await HasPrivilegeAsync(workerId, Offerings) || await IsCentreLeaderAsync(workerId, centreId);
    public async Task SeedAsync()
    {
        foreach (var definition in Definitions)
        {
            if (!await db.BricksPrivileges.AnyAsync(p => p.Code == definition.Code))
                db.BricksPrivileges.Add(new BricksPrivilege { Code = definition.Code, Name = definition.Name, Description = definition.Description });
        }
        await db.SaveChangesAsync();
    }
    public async Task SetAssignmentAsync(int actorId, int targetWorkerId, string code, bool grant)
    {
        if (!await IsAdministratorAsync(actorId)) throw new UnauthorizedAccessException("Only BRICKS Head or Assistant Head can assign privileges.");
        if (!await db.Workers.AnyAsync(w => w.Id == targetWorkerId && w.IsActive)) throw new InvalidOperationException("Select an active worker.");
        var privilege = await db.BricksPrivileges.SingleOrDefaultAsync(p => p.Code == code && p.IsActive)
            ?? throw new InvalidOperationException("Unknown or inactive BRICKS privilege.");
        var assignment = await db.BricksUserPrivileges.SingleOrDefaultAsync(p => p.WorkerId == targetWorkerId && p.BricksPrivilegeId == privilege.Id);
        if (assignment?.IsActive == grant) return;
        if (grant)
        {
            if (assignment == null)
                db.BricksUserPrivileges.Add(new BricksUserPrivilege { WorkerId = targetWorkerId, BricksPrivilegeId = privilege.Id, AssignedByWorkerId = actorId });
            else { assignment.IsActive = true; assignment.AssignedByWorkerId = actorId; assignment.AssignedAtUtc = DateTime.UtcNow; assignment.RevokedByWorkerId = null; assignment.RevokedAtUtc = null; }
        }
        else if (assignment != null) { assignment.IsActive = false; assignment.RevokedByWorkerId = actorId; assignment.RevokedAtUtc = DateTime.UtcNow; }
        db.BricksPrivilegeAudits.Add(new BricksPrivilegeAudit { WorkerId = targetWorkerId, ActorWorkerId = actorId, PrivilegeCode = code, Action = grant ? "Granted" : "Revoked" });
        await db.SaveChangesAsync();
    }
}

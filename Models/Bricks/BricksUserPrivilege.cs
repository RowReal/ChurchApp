namespace ChurchApp.Models.Bricks;
public class BricksUserPrivilege
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public ChurchApp.Models.Worker Worker { get; set; } = null!;
    public int BricksPrivilegeId { get; set; }
    public BricksPrivilege Privilege { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int AssignedByWorkerId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public int? RevokedByWorkerId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
public class BricksPrivilegeAudit
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public int ActorWorkerId { get; set; }
    public string PrivilegeCode { get; set; } = "";
    public string Action { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
namespace ChurchApp.Models.Finance;
public sealed class ExpenditureApprovalAction
{
    public int Id { get; set; }
    public int ExpenditureRecordId { get; set; }
    public ExpenditureRecord? ExpenditureRecord { get; set; }
    [Required, MaxLength(30)] public string Stage { get; set; } = "";
    [Required, MaxLength(30)] public string Decision { get; set; } = "";
    [Required, MaxLength(50)] public string WorkerId { get; set; } = "";
    [MaxLength(1000)] public string? Comments { get; set; }
    public DateTime ActionAtUtc { get; set; } = DateTime.UtcNow;
}

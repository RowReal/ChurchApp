using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChurchApp.Models.Finance;
public sealed class ExpenditureApprovalSettings
{
    public int Id { get; set; } = 1;
    [Column(TypeName="decimal(18,2)")]
    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal PastorApprovalThresholdNgn { get; set; } = 5000000m;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [StringLength(100)] public string? UpdatedByWorkerId { get; set; }
}

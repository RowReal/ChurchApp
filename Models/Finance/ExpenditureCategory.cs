using System;
using System.ComponentModel.DataAnnotations;
namespace ChurchApp.Models.Finance;
public sealed class ExpenditureCategory
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [StringLength(30)] public string? Code { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    public bool RequiresPurposeDescription { get; set; }
    public bool IsSystemOthers { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [StringLength(100)] public string? CreatedByWorkerId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    [StringLength(100)] public string? UpdatedByWorkerId { get; set; }
}

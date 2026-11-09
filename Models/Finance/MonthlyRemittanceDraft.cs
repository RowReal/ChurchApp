using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Finance;

public sealed class MonthlyRemittanceDraft
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft";
    [Required]
    public string SnapshotJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(100)]
    public string? CreatedByWorkerId { get; set; }
}

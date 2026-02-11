using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class AuditTrail
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string TableName { get; set; } = string.Empty; // e.g., "Worker"

        [Required]
        public int RecordId { get; set; } // The ID of the record that was modified

        [Required, MaxLength(20)]
        public string Action { get; set; } = string.Empty; // "CREATE", "UPDATE", "DELETE"

        [Required]
        public int ChangedByWorkerId { get; set; } // Who made the change

        [ForeignKey("ChangedByWorkerId")]
        public Worker ChangedByWorker { get; set; } = null!;

        // Store old values and new values as JSON (from your existing model)
        public string OldValues { get; set; } = string.Empty; // JSON of old values
        public string NewValues { get; set; } = string.Empty; // JSON of new values

        // Specific field that was changed (for detailed tracking) - from your existing model
        [MaxLength(100)]
        public string FieldName { get; set; } = string.Empty;

        // Enhanced description fields - combining both models
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty; // General description (from new model)

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty; // Additional notes (from your existing model)

        // Additional context information (from new model - optional)
        [MaxLength(100)]
        public string IpAddress { get; set; } = string.Empty;

        [MaxLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }

    // Helper class to track changes (keep your existing)
    public class PropertyChange
    {
        public string PropertyName { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
    }
}

// Models/AccountabilityCase.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class AccountabilityCase
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string CaseCode { get; set; } = GenerateCaseCode();

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        // Who is being held accountable (Subject)
        [Required]
        public int WorkerId { get; set; }
        [ForeignKey("WorkerId")]
        public virtual Worker Worker { get; set; }

        // Who created the case (usually Head of Directorate)
        [Required]
        public int CreatedByWorkerId { get; set; }
        [ForeignKey("CreatedByWorkerId")]
        public virtual Worker CreatedByWorker { get; set; }

        // Priority levels
        [MaxLength(20)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

        // Case status
        [MaxLength(20)]
        public string Status { get; set; } = "Open"; // Open, InProgress, Escalated, Resolved, Closed

        // Escalation level (1=Directorate, 2=Service, 3=Pastor)
        public int EscalationLevel { get; set; } = 1;

        // Who should respond next
        public int? AssignedToWorkerId { get; set; }
        [ForeignKey("AssignedToWorkerId")]
        public virtual Worker AssignedToWorker { get; set; }

        // Category of issue
        [MaxLength(50)]
        public string Category { get; set; } = "General"; // Attendance, Task, Conduct, Performance

        // Related task/service if applicable
        public int? RelatedTaskId { get; set; }
        [MaxLength(100)]
        public string RelatedTaskName { get; set; } = string.Empty;

        // Dates
        public DateTime OccurrenceDate { get; set; } = DateTime.Today;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public DateTime? LastUpdated { get; set; } = DateTime.UtcNow;

        // Confidential flag
        public bool IsConfidential { get; set; } = false;

        // For soft delete
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<AccountabilityMessage> Messages { get; set; } = new List<AccountabilityMessage>();
        public virtual ICollection<CaseAction> Actions { get; set; } = new List<CaseAction>();

        private static string GenerateCaseCode()
        {
            return $"AC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        [NotMapped]
        public string FormattedDueDate => DueDate?.ToString("dd MMM yyyy") ?? "No due date";

        [NotMapped]
        public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.Now &&
                                 Status != "Resolved" && Status != "Closed";
    }

    // Models/AccountabilityMessage.cs
    public class AccountabilityMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CaseId { get; set; }
        [ForeignKey("CaseId")]
        public virtual AccountabilityCase Case { get; set; }

        [Required]
        public int SenderWorkerId { get; set; }
        [ForeignKey("SenderWorkerId")]
        public virtual Worker SenderWorker { get; set; }

        [Required, MaxLength(20)]
        public string MessageType { get; set; } = "Question"; // Question, Response, Guidance, Warning, Escalation, Resolution

        [Required]
        public string Content { get; set; } = string.Empty;

        // Who can see this message (comma-separated levels: 1=Worker, 2=Directorate, 3=Service, 4=Pastor)
        [Required, MaxLength(50)]
        public string VisibleToLevels { get; set; } = "1,2";

        // If this is a response to another message
        public int? ReplyToMessageId { get; set; }
        [ForeignKey("ReplyToMessageId")]
        public virtual AccountabilityMessage ReplyToMessage { get; set; }

        // Attachments
        [MaxLength(500)]
        public string AttachmentPath { get; set; } = string.Empty;
        [MaxLength(100)]
        public string AttachmentName { get; set; } = string.Empty;

        // Confidential flag (only visible to same or higher levels)
        public bool IsConfidential { get; set; } = false;

        // Dates
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadDate { get; set; }

        [NotMapped]
        public string TimeAgo => GetTimeAgo(SentDate);

        [NotMapped]
        public string FormattedSentDate => SentDate.ToString("dd MMM yyyy hh:mm tt");

        private string GetTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.UtcNow - date;

            if (timeSpan.TotalMinutes < 1) return "just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 30) return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 365) return $"{(int)(timeSpan.TotalDays / 30)}mo ago";
            return $"{(int)(timeSpan.TotalDays / 365)}y ago";
        }
    }

    // Models/CaseAction.cs
    public class CaseAction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CaseId { get; set; }
        [ForeignKey("CaseId")]
        public virtual AccountabilityCase Case { get; set; }

        [Required]
        public int PerformedByWorkerId { get; set; }
        [ForeignKey("PerformedByWorkerId")]
        public virtual Worker PerformedByWorker { get; set; }

        [Required, MaxLength(20)]
        public string ActionType { get; set; } = "Create"; // Create, Update, Escalate, Assign, Resolve, Close, Reopen

        [Required]
        public string Description { get; set; } = string.Empty;

        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string FormattedActionDate => ActionDate.ToString("dd MMM yyyy HH:mm");
    }

    // Models/WorkerHierarchy.cs (New - to track hierarchy relationships)
    public class WorkerHierarchy
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WorkerId { get; set; }
        [ForeignKey("WorkerId")]
        public virtual Worker Worker { get; set; }

        // Hierarchy level based on Role
        // 1 = Worker (default), 2 = Head of Directorate, 3 = Head of Service, 4 = Pastor in Charge
        [Required]
        public int HierarchyLevel { get; set; } = 1;

        // If this worker is head of a directorate
        public int? DirectorateId { get; set; }
        [ForeignKey("DirectorateId")]
        public virtual Directorate Directorate { get; set; }

        // If this worker is head of a service (if you have Service model)
        public int? ServiceId { get; set; }
        [MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;

        // Reporting structure (immediate supervisor)
        public int? ReportsToWorkerId { get; set; }
        [ForeignKey("ReportsToWorkerId")]
        public virtual Worker ReportsToWorker { get; set; }

        // Direct subordinates (calculated field)
        [NotMapped]
        public List<Worker> Subordinates { get; set; } = new();

        // All workers under this hierarchy (including indirect)
        [NotMapped]
        public List<Worker> AllSubordinates { get; set; } = new();

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
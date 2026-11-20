using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.HouseFellowship
{
    public class HouseFellowshipCentre
    {
        public int Id { get; set; }
        [Required, MaxLength(20)] public string Code { get; set; } = "";
        [Required, MaxLength(150)] public string Name { get; set; } = "";
        [Required, MaxLength(150)] public string Location { get; set; } = "";
        [Required, MaxLength(500)] public string Address { get; set; } = "";
        [MaxLength(250)] public string? NearestLandmark { get; set; }
        [Required, MaxLength(150)] public string HostName { get; set; } = "";
        [Required, MaxLength(25)] public string HostPhone { get; set; } = "";
        public int LeaderWorkerId { get; set; }
        public Worker LeaderWorker { get; set; } = null!;
        public int AssistantWorkerId { get; set; }
        public Worker AssistantWorker { get; set; } = null!;
        [MaxLength(25)] public string? LeaderPhoneOverride { get; set; }
        public DateTime? StartedOn { get; set; }
        [Required, MaxLength(20)] public string Status { get; set; } = "Active";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public int? CreatedByWorkerId { get; set; }
    }

    public class HouseFellowshipMember
    {
        public int Id { get; set; }
        public int CentreId { get; set; }
        public HouseFellowshipCentre Centre { get; set; } = null!;
        [Required, MaxLength(100)] public string Surname { get; set; } = "";
        [Required, MaxLength(150)] public string OtherNames { get; set; } = "";
        [Required, MaxLength(25)] public string PhoneNumber { get; set; } = "";
        [MaxLength(25)] public string? WhatsAppNumber { get; set; }
        [EmailAddress, MaxLength(150)] public string? Email { get; set; }
        [Required, MaxLength(10)] public string Sex { get; set; } = ""; // Male, Female
        [Required, MaxLength(20)] public string AgeGroup { get; set; } = ""; // Child, Teenager, Adult, Elder
        [Required, MaxLength(25)] public string MaritalStatus { get; set; } = ""; // Single, Married, Divorced, Widow, Widower, Not Applicable
        public bool IsChurchWorker { get; set; }
        public int? DirectorateId { get; set; }
        public Directorate? Directorate { get; set; }
        public int? LinkedWorkerId { get; set; }
        public Worker? LinkedWorker { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime JoinedOn { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class HouseFellowshipSession
    {
        public int Id { get; set; }
        public int CentreId { get; set; }
        public HouseFellowshipCentre Centre { get; set; } = null!;
        public DateTime SessionDate { get; set; }
        [Required, MaxLength(20)] public string Status { get; set; } = "Scheduled"; // Scheduled, Submitted, Cancelled
        public DateTime? SubmittedAtUtc { get; set; }
        public int? SubmittedByWorkerId { get; set; }
        [MaxLength(500)] public string? Notes { get; set; }
    }

    public class HouseFellowshipAttendance
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public HouseFellowshipSession Session { get; set; } = null!;
        public int? MemberId { get; set; } // null for a visitor
        public HouseFellowshipMember? Member { get; set; }
        [MaxLength(150)] public string? VisitorName { get; set; }
        [MaxLength(10)] public string? VisitorSex { get; set; }
        [MaxLength(20)] public string? VisitorAgeGroup { get; set; }
        public bool IsPresent { get; set; }
        public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class HouseFellowshipOffering
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public HouseFellowshipSession Session { get; set; } = null!;
        [Range(0, double.MaxValue)] public decimal Amount { get; set; }
        public int RecordedByWorkerId { get; set; }
        public Worker RecordedByWorker { get; set; } = null!;
        public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class HouseFellowshipClosure
    {
        public int Id { get; set; }
        public DateTime FellowshipDate { get; set; }
        public int? CentreId { get; set; } // null means all centres
        public HouseFellowshipCentre? Centre { get; set; }
        [Required, MaxLength(500)] public string Reason { get; set; } = "";
        public int ClosedByWorkerId { get; set; }
        public Worker ClosedByWorker { get; set; } = null!;
        public DateTime ClosedAtUtc { get; set; } = DateTime.UtcNow;
        public bool IsRevoked { get; set; }
        public int? RevokedByWorkerId { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
    }

    public class HouseFellowshipLeadershipHistory
    {
        public int Id { get; set; }
        public int CentreId { get; set; }
        public HouseFellowshipCentre Centre { get; set; } = null!;
        public int WorkerId { get; set; }
        public Worker Worker { get; set; } = null!;
        [Required, MaxLength(20)] public string Position { get; set; } = ""; // Leader, Assistant
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class HouseFellowshipMemberTransfer
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public HouseFellowshipMember Member { get; set; } = null!;
        public int FromCentreId { get; set; }
        public int ToCentreId { get; set; }
        public DateTime TransferredAtUtc { get; set; } = DateTime.UtcNow;
        public int? TransferredByWorkerId { get; set; }
        [MaxLength(300)] public string? Reason { get; set; }
    }
}

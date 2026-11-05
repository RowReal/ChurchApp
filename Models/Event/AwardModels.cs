using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Event;

public class AwardProgramme
{
    public int Id { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = "2026 Workers' Awards";
    public int Year { get; set; } = 2026;
    public bool IsActive { get; set; } = true;
}

public class AwardCategory
{
    public int Id { get; set; }
    public int AwardProgrammeId { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(2500)] public string Description { get; set; } = "";
    [MaxLength(40)] public string NominationAudience { get; set; } = "AllWorkers"; // AllWorkers, Leaders, PastorInCharge
    [MaxLength(30)] public string NomineeType { get; set; } = "Worker"; // Worker, Directorate
    [MaxLength(20)] public string GenderFilter { get; set; } = "Any"; // Any, Male, Female
    public bool NominationAllowed { get; set; } = true;
    public bool RestrictNomineesToOwnDirectorate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime OpensAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ClosesAtUtc { get; set; } = DateTime.UtcNow.AddDays(14);
    public int MaxNominationsPerWorker { get; set; } = 1;
    [MaxLength(30)] public string SelectionRule { get; set; } = "Any"; // Any, OneMaleOneFemale
    public int SortOrder { get; set; }
}

public class AwardNomination
{
    public int Id { get; set; }
    public int AwardCategoryId { get; set; }
    public int NominatorWorkerId { get; set; } // Worker.Id
    public int SlotNumber { get; set; } = 1;
    public int? NomineeWorkerId { get; set; } // Worker.Id
    public int? NomineeDirectorateId { get; set; }
    public bool NoNomination { get; set; }
    [MaxLength(2000)] public string Justification { get; set; } = "";
    public bool IsSubmitted { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Event;

public class EventFunGame
{
    public int Id { get; set; }
    public int ChurchEventId { get; set; }
    [Required, MaxLength(120)] public string Title { get; set; } = "";
    [Required, MaxLength(20)] public string Kind { get; set; } = "FastestFinger";
    [MaxLength(250)] public string Phrase { get; set; } = "Jesus is Lord";
    public DateTime OpensAtUtc { get; set; }
    public DateTime ClosesAtUtc { get; set; }
    public bool IsPublished { get; set; }
    public bool QuizSubmissionsOpen { get; set; }
    public bool RevealAnswers { get; set; }
    public bool ShowLeaderboard { get; set; }
}
public class EventFunQuestion
{
    public int Id { get; set; }
    public int EventFunGameId { get; set; }
    public int Position { get; set; }
    [Required, MaxLength(500)] public string Text { get; set; } = "";
    [Required, MaxLength(250)] public string OptionA { get; set; } = "";
    [Required, MaxLength(250)] public string OptionB { get; set; } = "";
    [Required, MaxLength(250)] public string OptionC { get; set; } = "";
    [Required, MaxLength(250)] public string OptionD { get; set; } = "";
    [Required, MaxLength(1)] public string CorrectOption { get; set; } = "A";
    [MaxLength(120)] public string? BibleReference { get; set; }
}
public class EventFunEntry
{
    public int Id { get; set; }
    public int EventFunGameId { get; set; }
    [Required, MaxLength(20)] public string WorkerId { get; set; } = "";
    public int? EntryNumber { get; set; }
    public int? Score { get; set; }
    [MaxLength(4)] public string? Answers { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
}

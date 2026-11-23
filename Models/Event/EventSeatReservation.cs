using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Event;

// A reserved worker seat or a confirmed accompanying non-worker spouse seat.
public sealed class EventSeatReservation
{
    public int Id { get; set; }
    public int ChurchEventId { get; set; }
    public int EventTableId { get; set; }
    public int SeatNumber { get; set; }
    [Required, MaxLength(100)] public string OwnerWorkerId { get; set; } = "";
    [MaxLength(100)] public string? SpouseWorkerId { get; set; }
    [MaxLength(150)] public string? GuestName { get; set; }
    public int? GuestMealOptionId { get; set; }
    public bool IsGuest { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAtUtc { get; set; }
}

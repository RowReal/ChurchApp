using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Event
{
    public class EventCheckIn
    {
        public int Id { get; set; }
        public int ChurchEventId { get; set; }

        [Required, MaxLength(100)]
        public string WorkerId { get; set; } = string.Empty;

        public int EventTableId { get; set; }
        public int SeatNumber { get; set; }
        public int EventMenuOptionId { get; set; }
        public DateTime CheckedInAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? MenuUpdatedAtUtc { get; set; }
        public bool IsLeadershipSeat { get; set; }

        public ChurchEvent ChurchEvent { get; set; } = null!;
        public EventTable EventTable { get; set; } = null!;
        public EventMenuOption EventMenuOption { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Event
{
    public class EventTable
    {
        public int Id { get; set; }
        public int ChurchEventId { get; set; }

        [Required, MaxLength(80)]
        public string TableName { get; set; } = string.Empty;

        public int Capacity { get; set; } = 8;
        public bool IsLeadershipReserved { get; set; }
        public bool IsActive { get; set; } = true;
    }
}


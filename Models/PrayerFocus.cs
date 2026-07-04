using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class PrayerFocus
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Theme { get; set; } = "";

        [StringLength(500)]
        public string? BibleVerse { get; set; }

        public DateTime WeekStartDate { get; set; } = DateTime.Today;

        public DateTime WeekEndDate { get; set; } = DateTime.Today.AddDays(6);

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public List<PrayerPoint> PrayerPoints { get; set; } = new();
    }
}
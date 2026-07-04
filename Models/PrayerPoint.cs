using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class PrayerPoint
    {
        public int Id { get; set; }

        public int PrayerFocusId { get; set; }

        public PrayerFocus? PrayerFocus { get; set; }

        [Required]
        [StringLength(500)]
        public string PointText { get; set; } = "";

        public int DisplayOrder { get; set; } = 1;
    }
}

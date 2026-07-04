using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class VerseOfTheDay
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string VerseText { get; set; } = "";

        [Required]
        [StringLength(150)]
        public string ScriptureReference { get; set; } = "";

        public DateTime DisplayDate { get; set; } = DateTime.Today;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
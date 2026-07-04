using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class ChurchNotice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = "";

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = "";

        public NoticeType Type { get; set; } = NoticeType.Announcement;

        public DateTime PublishDate { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiryDate { get; set; }

        public bool IsPinned { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class Unit
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department Department { get; set; } = null!;

        // Unit Leader (ADD THIS PROPERTY)
        public int? LeaderWorkerId { get; set; }

        // Unit Leader Navigation Property (ADD THIS)
        [ForeignKey("LeaderWorkerId")]
        public Worker? LeaderWorker { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Worker> Workers { get; set; } = new List<Worker>();
    }
}
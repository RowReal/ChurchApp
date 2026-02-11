using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class Directorate
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        // Head of Directorate
        public int? HeadWorkerId { get; set; }

        [ForeignKey("HeadWorkerId")]
        public Worker? HeadWorker { get; set; }

        // Assistant Head of Directorate
        public int? AssistantHeadWorkerId { get; set; }

        [ForeignKey("AssistantHeadWorkerId")]
        public Worker? AssistantHeadWorker { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
        public virtual ICollection<Worker> Workers { get; set; } = new List<Worker>();
    }
}
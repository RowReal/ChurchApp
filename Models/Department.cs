using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        // Foreign key to Directorate
        public int DirectorateId { get; set; }

        // Navigation property
        public Directorate? Directorate { get; set; }

        // Head of Department
        public int? HeadWorkerId { get; set; }
        public Worker? HeadWorker { get; set; }

        // Assistant Head of Department (NEW)
        public int? AssistantHeadWorkerId { get; set; }
        public Worker? AssistantHeadWorker { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        // Navigation Properties
        public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();
        public virtual ICollection<Worker> Workers { get; set; } = new List<Worker>();

    }

    // Model for the form
    public class DepartmentModel
    {
        [Required(ErrorMessage = "Department name is required")]
        [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department code is required")]
        [MaxLength(10, ErrorMessage = "Code cannot exceed 10 characters")]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a directorate")]
        public int DirectorateId { get; set; }

        public int? HeadWorkerId { get; set; }
        public int? AssistantHeadWorkerId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
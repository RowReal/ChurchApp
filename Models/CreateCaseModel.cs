using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class CreateCaseModel
    {
        [Required(ErrorMessage = "Worker is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a worker")]
        public int WorkerId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; } = "General";

        [Required(ErrorMessage = "Priority is required")]
        public string Priority { get; set; } = "Medium";

        public DateTime OccurrenceDate { get; set; } = DateTime.Today;
        public DateTime? DueDate { get; set; }
        public bool IsConfidential { get; set; } = false;
    }
}

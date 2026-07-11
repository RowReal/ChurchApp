using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class LeaveRequestFormModel
    {
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Please select a relieve officer.")]
        public int? RelieveOfficerId { get; set; }

        [Required(ErrorMessage = "Purpose is required.")]
        [MinLength(
            10,
            ErrorMessage = "Please provide a more detailed purpose.")]
        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string PendingAssignments { get; set; } =
            string.Empty;

        [MaxLength(250)]
        public string AssignmentHandler { get; set; } =
            string.Empty;

        public int DurationInDays
        {
            get
            {
                if (!StartDate.HasValue ||
                    !EndDate.HasValue)
                {
                    return 0;
                }

                return
                    (EndDate.Value.Date -
                     StartDate.Value.Date).Days + 1;
            }
        }

        public void Clear()
        {
            StartDate = null;
            EndDate = null;
            RelieveOfficerId = null;
            Purpose = string.Empty;
            PendingAssignments = string.Empty;
            AssignmentHandler = string.Empty;
        }
    }
}
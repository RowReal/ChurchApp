using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class ProfileUpdateApprover
    {
        public int Id { get; set; }

        [Required]
        public int ProfileUpdateRequestId { get; set; }

        [ForeignKey(nameof(ProfileUpdateRequestId))]
        public ProfileUpdateRequest ProfileUpdateRequest
        {
            get;
            set;
        } = null!;

        [Required]
        public int ApproverWorkerId { get; set; }

        [ForeignKey(nameof(ApproverWorkerId))]
        public Worker ApproverWorker
        {
            get;
            set;
        } = null!;

        public bool HasActed { get; set; }

        [MaxLength(20)]
        public string? Decision { get; set; }

        public DateTime? DecisionDate { get; set; }
    }
}
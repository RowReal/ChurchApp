using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class UpdateSecondTimerModel
    {
        [Required(ErrorMessage = "Guest ID is required")]
        public int GuestId { get; set; }

        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string CurrentPhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please specify if you want to become a member")]
        public bool? WantsToBecomeMember { get; set; }

        [Required(ErrorMessage = "Please select birth month")]
        public string BirthMonth { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please specify if you've been baptised by water")]
        public bool? IsBaptisedByWater { get; set; }

        [Required(ErrorMessage = "Please specify if you want to join the workforce")]
        public bool? WantsToJoinWorkforce { get; set; }
    }
}
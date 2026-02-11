using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class CreateGuestModel
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Middle name cannot exceed 100 characters")]
        public string MiddleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [MaxLength(100, ErrorMessage = "Surname cannot exceed 100 characters")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sex is required")]
        public string Sex { get; set; } = string.Empty;

        [Required(ErrorMessage = "Age group is required")]
        public string AgeGroup { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "Landmark cannot exceed 200 characters")]
        public string Landmark { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "WhatsApp number cannot exceed 20 characters")]
        public string WhatsAppNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please specify if you're an RCCG member")]
        public bool IsRCCGMember { get; set; } = false;

        public string OtherChurch { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please specify how you found us")]
        public string HowFoundUs { get; set; } = string.Empty;

        public string InvitedByName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a service")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a service")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Visiting date is required")]
        public DateTime VisitingDate { get; set; } = DateTime.Today;
    }
}
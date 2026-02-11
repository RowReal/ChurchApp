using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class AuthState
    {
        public bool IsAuthenticated { get; set; }
        public Worker? CurrentWorker { get; set; }
        public string Token { get; set; } = string.Empty;
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Worker ID is required")]
        public string WorkerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordModel
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Worker ID is required")]
        public string WorkerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;
    }

    public class CreateWorkerModel
    {
        [Required(ErrorMessage = "Worker ID is required")]
        public string WorkerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required")]
        public string LastName { get; set; } = string.Empty;

        public string MiddleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Directorate is required")]
        public int DirectorateId { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public int DepartmentId { get; set; }

        public int? UnitId { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;
        // Add this Role property
        public string Role { get; set; } = "Worker";
    }
}
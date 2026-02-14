namespace ChurchApp.Services
{
    public class PasswordResetResult
    {
        public bool Success { get; set; }
        public string? GeneratedPassword { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
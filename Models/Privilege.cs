namespace ChurchApp.Models
{
    public class Privilege
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty; // Example: Access-Guest-Management

        public string Name { get; set; } = string.Empty; // Example: Access Guest Management

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

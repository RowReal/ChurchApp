namespace ChurchApp.Models
{
    public class UserPrivilege
    {
        public int Id { get; set; }

        public int WorkerId { get; set; }
        public Worker? Worker { get; set; }

        public int PrivilegeId { get; set; }
        public Privilege? Privilege { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public string? AssignedBy { get; set; }
    }
}
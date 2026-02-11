// Models/Role.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(1, 100)]
        public int Level { get; set; } = 10; // Default level

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        public string? UpdatedBy { get; set; }
    }
}

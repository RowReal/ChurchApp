
using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Finance
{
    public class RemittanceElement
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Remittance element name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Code { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        [StringLength(150)]
        public string? LastModifiedBy { get; set; }
    }
}

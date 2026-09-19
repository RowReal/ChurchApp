using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Finance
{
    public class IncomeCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Code { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        [StringLength(150)]
        public string? LastModifiedBy { get; set; }

        public ICollection<IncomeType> IncomeTypes { get; set; }
            = new List<IncomeType>();
    }
}
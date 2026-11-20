using System.ComponentModel.DataAnnotations;
namespace ChurchApp.Models.Bricks;
public class BricksPrivilege
{
    public int Id { get; set; }
    [Required, MaxLength(80)] public string Code { get; set; } = "";
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(300)] public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

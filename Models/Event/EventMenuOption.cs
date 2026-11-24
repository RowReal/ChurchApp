using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Event;

public class EventMenuOption
{
    public int Id { get; set; }
    public int ChurchEventId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int? Quantity { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int AllocatedQuantity { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int? RemainingQuantity => Quantity.HasValue ? Math.Max(0, Quantity.Value - AllocatedQuantity) : null;

    public bool IsActive { get; set; } = true;
}

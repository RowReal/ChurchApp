using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Event;

public class EventMenuOption
{
    public int Id { get; set; }
    public int ChurchEventId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

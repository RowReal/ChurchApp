namespace ChurchApp.Models.Event
{
    public class EventSeatingSettings
    {
        public int Id { get; set; }
        public int ChurchEventId { get; set; }
        public bool MixDirectorates { get; set; } = true;
    }
}


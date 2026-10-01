namespace ChurchApp.Models
{
    public class BroadcastAudienceDirectorate
    {
        public int Id { get; set; }

        public int BroadcastMessageId { get; set; }
        public BroadcastMessage? BroadcastMessage { get; set; }

        public int DirectorateId { get; set; }
        public Directorate? Directorate { get; set; }
    }
}
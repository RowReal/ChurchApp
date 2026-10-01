using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class BroadcastMessage
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string MessageBody { get; set; } = string.Empty;

        // PSO, MEAT, ClusterHead, DirectorateHead
        [Required]
        [MaxLength(50)]
        public string SenderType { get; set; } = string.Empty;

        // Recipient-facing label:
        // PSO, MEAT, Asst Pastor, Directorate Head
        [Required]
        [MaxLength(100)]
        public string SenderDisplayLabel { get; set; } = string.Empty;

        // AllWorkers, AllLeaders, SelectedDirectorates, OwnDirectorate
        [Required]
        [MaxLength(50)]
        public string AudienceType { get; set; } = string.Empty;

        public int SentByWorkerId { get; set; }
        public Worker? SentByWorker { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool IsSent { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<BroadcastRecipient> Recipients { get; set; }
            = new List<BroadcastRecipient>();

        public ICollection<BroadcastAudienceDirectorate> AudienceDirectorates { get; set; }
            = new List<BroadcastAudienceDirectorate>();
    }
}
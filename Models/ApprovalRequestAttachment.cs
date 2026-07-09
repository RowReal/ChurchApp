namespace ChurchApp.Models
{
    public class ApprovalRequestAttachment
    {
        public int Id { get; set; }

        public int ApprovalRequestId { get; set; }
        public ApprovalRequest? ApprovalRequest { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string? FileType { get; set; }

        public long FileSize { get; set; }

        public int UploadedByWorkerId { get; set; }
        public Worker? UploadedByWorker { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
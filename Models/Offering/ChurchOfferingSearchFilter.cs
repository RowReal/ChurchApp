namespace ChurchApp.Models
{
    public class ChurchOfferingSearchFilter
    {
        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        public int? ServiceId { get; set; }

        public int? OfferingTypeId { get; set; }

        public string? Currency { get; set; }

        public string? PaymentMode { get; set; }

        public string? Status { get; set; }

        public string? RecordedByWorkerId { get; set; }

        public string? SearchText { get; set; }

        public bool IncludeRemoved { get; set; }

        public void Clear()
        {
            DateFrom = null;
            DateTo = null;
            ServiceId = null;
            OfferingTypeId = null;
            Currency = null;
            PaymentMode = null;
            Status = null;
            RecordedByWorkerId = null;
            SearchText = null;
            IncludeRemoved = false;
        }
    }
}
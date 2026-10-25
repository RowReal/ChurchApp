using System;

namespace ChurchApp.Models
{
    public class ChurchCollectionSearchFilter
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? ServiceId { get; set; }
        public int? IncomeTypeId { get; set; }
        public string? Currency { get; set; }
        public string? PaymentMode { get; set; }
        public string? Status { get; set; }
        public string? RecordedByWorkerId { get; set; }
        public string? SearchText { get; set; }
        public bool IncludeRemoved { get; set; }
    }
}

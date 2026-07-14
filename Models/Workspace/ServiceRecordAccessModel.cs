namespace ChurchApp.Models
{
    public class ServiceRecordAccessModel
    {
        public bool CanRecordOffering { get; set; }

        public bool CanApproveOffering { get; set; }

        public bool CanViewOfferingReport { get; set; }

        public bool CanRequestOfferingAmendment { get; set; }

        public bool CanApproveOfferingAmendment { get; set; }

        public bool CanRecordAttendance { get; set; }

        public bool CanRecordVehicle { get; set; }

        public bool CanAccessServiceNotes { get; set; }

        public bool CanAccessGuestManagement { get; set; }

        public int PendingOfferingApprovalCount { get; set; }

        public int PendingOfferingAmendmentCount { get; set; }

        public bool HasAnyAccess =>
            CanRecordOffering ||
            CanApproveOffering ||
            CanViewOfferingReport ||
            CanRequestOfferingAmendment ||
            CanApproveOfferingAmendment ||
            CanRecordAttendance ||
            CanRecordVehicle ||
            CanAccessServiceNotes ||
            CanAccessGuestManagement;
    }
}
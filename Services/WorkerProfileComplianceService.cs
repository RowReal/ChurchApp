using ChurchApp.Models;

namespace ChurchApp.Services
{
    public class WorkerProfileComplianceService
    {
        private const int TotalMarks = 18;

        /// <summary>
        /// Calculates the standard BCC worker profile compliance percentage.
        /// This calculation should be used anywhere profile compliance or
        /// profile completion is displayed in ServiceHub.
        /// </summary>
        public int Calculate(Worker worker)
        {
            if (worker == null)
                return 0;

            var marks = 0;

            // 1. Title
            if (!string.IsNullOrWhiteSpace(worker.Title))
                marks++;

            // 2. First Name
            if (!string.IsNullOrWhiteSpace(worker.FirstName))
                marks++;

            // 3. Last Name
            if (!string.IsNullOrWhiteSpace(worker.LastName))
                marks++;

            // 4. Sex
            if (!string.IsNullOrWhiteSpace(worker.Sex))
                marks++;

            // 5. Email
            if (!string.IsNullOrWhiteSpace(worker.Email))
                marks++;

            // 6. Phone
            if (!string.IsNullOrWhiteSpace(worker.Phone))
                marks++;

            // 7. Date of Birth
            if (worker.DateOfBirth.HasValue)
                marks++;

            // 8. Marital Status
            if (!string.IsNullOrWhiteSpace(worker.MaritalStatus))
                marks++;

            // 9. Profession
            if (!string.IsNullOrWhiteSpace(worker.Profession))
                marks++;

            // 10. Organization
            if (!string.IsNullOrWhiteSpace(worker.Organization))
                marks++;

            // 11. Address
            if (!string.IsNullOrWhiteSpace(worker.Address))
                marks++;

            // 12. Directorate
            if (worker.DirectorateId.HasValue)
                marks++;

            // 13. Department
            //
            // Normally, a Department must be selected.
            // However, the following leadership roles receive the
            // Department mark automatically because a Department
            // assignment may not apply to them.
            if (worker.DepartmentId.HasValue || IsDepartmentExemptRole(worker.Role))
                marks++;

            // 14. Role
            if (!string.IsNullOrWhiteSpace(worker.Role))
                marks++;

            // 15. Date Joined Church / BCC
            if (worker.DateJoinedChurch.HasValue)
                marks++;

            // 16. Ordination Level
            if (!string.IsNullOrWhiteSpace(worker.OrdinationLevel))
                marks++;

            // 17. Church Training / Qualification
            //
            // One mark is awarded if at least one of the existing
            // recognised church training/qualification records is present.
            if (worker.HasBelieverBaptism ||
                worker.HasWorkerInTraining ||
                worker.HasSOD ||
                worker.HasBibleCollege)
            {
                marks++;
            }

            // 18. Passport Photograph
            if (!string.IsNullOrWhiteSpace(worker.PassportPhotoPath))
                marks++;

            return (int)Math.Round(
                (double)marks / TotalMarks * 100);
        }

        /// <summary>
        /// Returns the standard progress-bar class for profile compliance.
        /// Below 50% = Red
        /// 50% - 85% = Warning
        /// 86% and above = Green
        /// </summary>
        public string GetProgressClass(int percentage)
        {
            if (percentage < 50)
                return "bg-danger";

            if (percentage < 86)
                return "bg-warning";

            return "bg-success";
        }

        private static bool IsDepartmentExemptRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            var normalizedRole = role.Trim().ToLowerInvariant();

            return normalizedRole switch
            {
                "head of directorate" => true,
                "asst head of directorate" => true,
                "assistant head of directorate" => true,

                "head of service" => true,
                "asst head of service" => true,
                "assistant head of service" => true,

                "church admin" => true,
                "pastor in charge" => true,

                _ => false
            };
        }
    }
}
using System;

namespace ChurchApp.Services.Event
{
    public enum EventSeatingCategory
    {
        Excluded,
        Leadership,
        General
    }

    public static class EventSeatingEligibility
    {
        public static EventSeatingCategory Classify(string? role)
        {
            var value = (role ?? string.Empty).Trim();

            if (Is(value, "Senior Pastor") ||
                Is(value, "Church Admin") ||
                Is(value, "Pastor in Charge") ||
                Is(value, "Council Member"))
                return EventSeatingCategory.Excluded;

            if (Is(value, "Head of Directorate") ||
                Is(value, "Assistant Head of Directorate") ||
                Is(value, "Asst Head of Directorate") ||
                Is(value, "Head of Service") ||
                Is(value, "Assistant Head of Service") ||
                Is(value, "Asst Head of Service"))
                return EventSeatingCategory.Leadership;

            return EventSeatingCategory.General;
        }

        private static bool Is(string actual, string expected) =>
            string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }
}

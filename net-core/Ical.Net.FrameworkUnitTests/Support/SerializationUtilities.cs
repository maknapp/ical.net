using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;

namespace Ical.Net.FrameworkUnitTests.Support
{
    internal static class SerializationUtilities
    {
        public static string SerializeEventToString(CalendarEvent calendarEvent)
        {
            return SerializeCalenderToString(new Calendar
            {
                Events = {calendarEvent}
            });
        }

        public static string SerializeCalenderToString(Calendar calendar)
            => new CalendarSerializer().SerializeToString(calendar);
    }
}

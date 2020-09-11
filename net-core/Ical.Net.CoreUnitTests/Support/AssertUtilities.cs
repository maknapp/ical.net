using NUnit.Framework;

namespace Ical.Net.FrameworkUnitTests.Support
{
    public static class AssertUtilities
    {
        public static void AssertCalendar(Calendar calendar)
        {
            Assert.IsNotNull(calendar, "The iCalendar was not loaded");

            if (calendar.Events.Count > 0)
            {
                Assert.IsTrue(calendar.Events.Count == 1,
                    $"Calendar should contain 1 event; however, the iCalendar loaded {calendar.Events.Count} events");
            }
            else if (calendar.Todos.Count > 0)
            {
                Assert.IsTrue(calendar.Todos.Count == 1,
                    $"Calendar should contain 1 todo; however, the iCalendar loaded {calendar.Todos.Count} todos");
            }
        }
    }
}

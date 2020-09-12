using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using static Ical.Net.FrameworkUnitTests.Support.SerializationUtilities;

namespace Ical.Net.FrameworkUnitTests
{
    public class SymmetricSerializationTests
    {
        private const string LdapUri = "ldap://example.com:6666/o=eDABC Industries,c=3DUS??(cn=3DBMary Accepted)";

        private static readonly DateTime Now = new DateTime(2010, 11, 12, 05, 06, 07);
        private static readonly DateTime Later = Now.AddHours(1);
        
        [Test, TestCaseSource(nameof(Event_TestCases))]
        public void Event_Tests(Calendar iCalendar)
        {
            var originalEvent = iCalendar.Events.Single();

            var serializedCalendar = SerializeCalenderToString(iCalendar);
            var unserializedCalendar = Calendar.Load(serializedCalendar);

            var onlyEvent = unserializedCalendar.Events.Single();

            Assert.AreEqual(originalEvent.GetHashCode(), onlyEvent.GetHashCode());
            Assert.AreEqual(originalEvent, onlyEvent);
            Assert.AreEqual(iCalendar, unserializedCalendar);
        }

        public static IEnumerable<ITestCaseData> Event_TestCases()
        {
            var rrule = new RecurrencePattern(FrequencyType.Daily, 1) { Count = 5 };
            var e = new CalendarEvent
            {
                DtStart = new CalDateTime(Now),
                DtEnd = new CalDateTime(Later),
                Duration = TimeSpan.FromHours(1),
                RecurrenceRules = new List<RecurrencePattern> { rrule },
            };

            var calendar = new Calendar();
            calendar.Events.Add(e);
            yield return new TestCaseData(calendar).SetName("readme.md example");

            e = CreateValidEvent();
            e.Description = "This is an event description that is really rather long. Hopefully the line breaks work now, and it's serialized properly.";
            calendar = new Calendar();
            calendar.Events.Add(e);
            yield return new TestCaseData(calendar).SetName("Description serialization isn't working properly. Issue #60");
        }

        [Test]
        public void VTimeZoneSerialization_Test()
        {
            var originalCalendar = new Calendar();
            var tz = new VTimeZone
            {
                TzId = "New Zealand Standard Time"
            };
            originalCalendar.AddTimeZone(tz);
            var serializer = new CalendarSerializer();
            var serializedCalendar = serializer.SerializeToString(originalCalendar);
            var unserializedCalendar = Calendar.Load(serializedCalendar);

            CollectionAssert.AreEqual(originalCalendar.TimeZones, unserializedCalendar.TimeZones);
            Assert.AreEqual(originalCalendar, unserializedCalendar);
            Assert.AreEqual(originalCalendar.GetHashCode(), unserializedCalendar.GetHashCode());
        }

        [Test, TestCaseSource(nameof(AttendeeSerialization_TestCases))]
        public void AttendeeSerialization_Test(Attendee attendee)
        {
            var calendar = new Calendar();
            calendar.AddTimeZone(new VTimeZone("America/Los_Angeles"));
            var someEvent = CreateValidEvent();
            someEvent.Attendees = new List<Attendee> {attendee};
            calendar.Events.Add(someEvent);

            var serialized = SerializeCalenderToString(calendar);
            var unserialized = Calendar.Load(serialized);

            Assert.AreEqual(calendar.GetHashCode(), unserialized.GetHashCode());
            Assert.IsTrue(calendar.Events.SequenceEqual(unserialized.Events));
            Assert.AreEqual(calendar, unserialized);
        }

        public static IEnumerable<ITestCaseData> AttendeeSerialization_TestCases()
        {
            var complex1 = new Attendee("MAILTO:mary@example.com")
            {
                CommonName = "Mary Accepted",
                Rsvp = true,
                ParticipationStatus = EventParticipationStatus.Accepted,
                SentBy = new Uri("mailto:someone@example.com"),
                DirectoryEntry = new Uri(LdapUri),
                Type = "CuType",
                Members = new List<string> { "Group A", "Group B" },
                Role = ParticipationRole.Chair,
                DelegatedTo = new List<string> { "Peon A", "Peon B" },
                DelegatedFrom = new List<string> { "Bigwig A", "Bigwig B" }
            };
            yield return new TestCaseData(complex1).SetName("Complex attendee");

            var simple = new Attendee("MAILTO:james@example.com")
            {
                CommonName = "James James",
                Role = ParticipationRole.RequiredParticipant,
                Rsvp = true,
                ParticipationStatus = EventParticipationStatus.Tentative
            };
            yield return new TestCaseData(simple).SetName("Simple attendee");
        }

        [Test, TestCaseSource(nameof(BinaryAttachment_TestCases))]
        public void BinaryAttachment_Tests(string theString, string expectedAttachment)
        {
            var asBytes = Encoding.UTF8.GetBytes(theString);
            var binaryAttachment = new Attachment(asBytes);

            var calendar = new Calendar();
            var vEvent = CreateValidEvent();
            vEvent.Attachments = new List<Attachment> { binaryAttachment };
            calendar.Events.Add(vEvent);

            var serialized = SerializeCalenderToString(calendar);
            var unserialized = Calendar.Load(serialized);
            var unserializedAttachment = unserialized
                .Events
                .First()
                .Attachments
                .Select(a => Encoding.UTF8.GetString(a.Data))
                .First();

            Assert.AreEqual(expectedAttachment, unserializedAttachment);
            Assert.AreEqual(calendar.GetHashCode(), unserialized.GetHashCode());
            Assert.AreEqual(calendar, unserialized);
        }

        public static IEnumerable<ITestCaseData> BinaryAttachment_TestCases()
        {
            const string shortString = "This is a string.";
            yield return new TestCaseData(shortString, shortString)
                .SetName("Short string");

            const string mediumString = "This is a stringThisThis is a";
            yield return new TestCaseData(mediumString, mediumString)
                .SetName("Moderate string");

            const string longishString = "This is a song that never ends. It just goes on and on my friends. Some people started singing it not...";
            yield return new TestCaseData(longishString, longishString)
                .SetName("Much longer string");

            const string jsonString =
                "{\"TheList\":[\"Foo\",\"Bar\",\"Baz\",\"Foo\",\"Bar\",\"Baz\",\"Foo\",\"Bar\",\"Baz\",\"Foo\",\"Bar\",\"Baz\",\"Foo\",\"Bar\",\"Baz\",\"Foo\",\"Bar\",\"Baz\"],\"TheNumber\":42,\"TheSet\":[\"Foo\",\"Bar\",\"Baz\"]}";
            yield return new TestCaseData(jsonString, jsonString).SetName("JSON-serialized text");
        }

        [Test, TestCaseSource(nameof(UriAttachment_TestCases))]
        public void UriAttachment_Tests(string uri, Uri expectedUri)
        {
            var attachment = new Attachment(uri);

            var calendar = new Calendar();
            var vEvent = CreateValidEvent();
            vEvent.Attachments = new List<Attachment> { attachment };
            calendar.Events.Add(vEvent);

            var serialized = SerializeCalenderToString(calendar);
            var unserialized = Calendar.Load(serialized);
            var unserializedUri = unserialized
                .Events
                .First()
                .Attachments
                .Select(a => a.Uri)
                .Single();

            Assert.AreEqual(expectedUri, unserializedUri);
            Assert.AreEqual(calendar.GetHashCode(), unserialized.GetHashCode());
            Assert.AreEqual(calendar, unserialized);
        }

        public static IEnumerable<ITestCaseData> UriAttachment_TestCases()
        {
            yield return new TestCaseData("http://www.google.com", new Uri("http://www.google.com")).SetName("HTTP URL");
            yield return new TestCaseData("mailto:rstockbower@gmail.com", new Uri("mailto:rstockbower@gmail.com")).SetName("mailto: URL");
            yield return new TestCaseData(LdapUri, new Uri(LdapUri)).SetName("ldap URL");
            yield return new TestCaseData("C:\\path\\to\\file.txt", new Uri("C:\\path\\to\\file.txt")).SetName("Local file path URL");
            yield return new TestCaseData(@"\\uncPath\to\resource.txt", new Uri(@"\\uncPath\to\resource.txt")).SetName("UNC path URL");
        }

        [Test, Ignore("TODO: Fix CATEGORIES multiple serializations")]
        public void CategoriesTest()
        {
            var vEvent = CreateValidEvent();
            vEvent.Categories = new List<string> { "Foo", "Bar", "Baz" };
            var c = new Calendar();
            c.Events.Add(vEvent);

            var serialized = SerializeCalenderToString(c);
            var categoriesCount = Regex.Matches(serialized, "CATEGORIES").Count;
            Assert.AreEqual(1, categoriesCount);

            var deserialized = Calendar.Load(serialized);
            Assert.AreEqual(vEvent, deserialized);
        }

        private static CalendarEvent CreateValidEvent()
        {
            return new CalendarEvent
            {
                DtStart = new CalDateTime(Now),
                DtEnd = new CalDateTime(Later),
                Duration = Later - Now
            };
        }
    }
}

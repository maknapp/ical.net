using System;
using System.Collections.Generic;

namespace Ical.Net.Serialization
{
    public sealed class CalendarSerializer : ComponentSerializer
    {
        public CalendarSerializer()
            :this(new SerializationContext()) { }

        public CalendarSerializer(SerializationContext ctx) : base(ctx) {}

        public override IComparer<ICalendarProperty> PropertySorter => new CalendarPropertySorter();

        public override string Serialize(object obj)
        {
            var calendar = obj as Calendar;
            if (calendar != null)
            {
                // If we're serializing a calendar, we should indicate that we're using ical.net to do the work
                calendar.Version = LibraryMetadata.Version;
                calendar.ProductId = LibraryMetadata.ProdId;

                return base.Serialize(calendar);
            }

            return base.Serialize(obj);
        }

        public override object Deserialize(string value) => null;

        private class CalendarPropertySorter : IComparer<ICalendarProperty>
        {
            public int Compare(ICalendarProperty x, ICalendarProperty y)
            {
                if (x == y)
                {
                    return 0;
                }
                if (x == null)
                {
                    return -1;
                }
                if (y == null)
                {
                    return 1;
                }
                // Alphabetize all properties except VERSION, which should appear first. 
                if (string.Equals("VERSION", x.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return -1;
                }
                return string.Equals("VERSION", y.Name, StringComparison.OrdinalIgnoreCase)
                    ? 1
                    : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}

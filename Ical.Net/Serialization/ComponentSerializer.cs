//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ical.Net.CalendarComponents;
using Ical.Net.Utility;

namespace Ical.Net.Serialization;

public class ComponentSerializer : SerializerBase
{
    protected virtual IComparer<ICalendarProperty> PropertySorter => new PropertyAlphabetizer();

    public ComponentSerializer() { }

    public ComponentSerializer(SerializationContext ctx) : base(ctx) { }

    public override Type TargetType => typeof(CalendarComponent);

    public override string? SerializeToString(object? obj)
    {
        if (obj is not CalendarComponent c)
        {
            return null;
        }

        return Serialization2.CalendarSerializer.Serialize(c);
    }

    public override object? Deserialize(TextReader tr) => null;

    public class PropertyAlphabetizer : IComparer<ICalendarProperty>
    {
        public int Compare(ICalendarProperty? x, ICalendarProperty? y)
        {
            if (x == y)
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            return y == null
                ? 1
                : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}

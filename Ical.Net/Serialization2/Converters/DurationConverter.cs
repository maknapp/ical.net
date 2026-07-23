//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization2.Converters;

internal class DurationConverter : CalendarPropertyConverter<Duration>
{
    public override Duration Read(CalendarReader reader, IParameterCollection parameters)
    {
        var value = reader.GetTextValue();

        if (Duration.TryParse(value, out var duration))
        {
            return duration;
        }

        throw new FormatException("String value is not in the ISO 8601 basic format for DURATION");
    }

    public override void Write(CalendarWriter writer, Duration value)
    {
        writer.WriteValue(value.ToBasicIso());
    }
}

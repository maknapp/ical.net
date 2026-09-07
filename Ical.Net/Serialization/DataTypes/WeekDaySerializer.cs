//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.IO;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes;

public class WeekDaySerializer : EncodableDataTypeSerializer
{
    public WeekDaySerializer() { }

    public WeekDaySerializer(SerializationContext ctx) : base(ctx) { }

    public override Type TargetType => typeof(WeekDay);

    public override string? SerializeToString(object? obj)
    {
        if (obj is not WeekDay ds)
        {
            return null;
        }

        var value = string.Empty;
        if (ds.Offset.HasValue)
        {
            value += ds.Offset;
        }

        try
        {
            var name = Enum.GetName(typeof(DayOfWeek), ds.DayOfWeek);
            if (name == null) return null;

            value += name.ToUpperInvariant().Substring(0, 2);
        }
        catch
        {
            return null;
        }

        return Encode(ds, value);
    }

    public override object? Deserialize(TextReader tr)
    {
        var value = tr.ReadToEnd();

        if (WeekDay.TryParse(value, out var weekDay))
        {
            return weekDay;
        }

        return null;
    }
}

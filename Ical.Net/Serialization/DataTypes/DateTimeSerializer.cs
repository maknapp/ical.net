//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.IO;
using Ical.Net.DataTypes;
using Ical.Net.Utility;

namespace Ical.Net.Serialization.DataTypes;

/// <summary>
/// A serializer for the <see cref="CalDateTime"/> data type.
/// </summary>
public class DateTimeSerializer : SerializerBase, IParameterProvider
{
    /// <summary>
    /// This constructor is required for the SerializerFactory to work.
    /// </summary>
    public DateTimeSerializer() { }

    /// <summary>
    /// Creates a new instance of the <see cref="DateTimeSerializer"/> class.
    /// </summary>
    /// <param name="ctx"></param>
    public DateTimeSerializer(SerializationContext ctx) : base(ctx) { }

    public override Type TargetType => typeof(CalDateTime);

    public override string? SerializeToString(object? obj)
    {
        if (obj is not CalDateTime dt)
        {
            return null;
        }

        // RFC 5545 3.3.5:
        // The date with UTC time, or absolute time, is identified by a LATIN
        // CAPITAL LETTER Z suffix character, the UTC designator, appended to
        // the time value. The "TZID" property parameter MUST NOT be applied to DATE-TIME
        // properties whose time values are specified in UTC.

        return dt.ToBasicIso();
    }

    public override object? Deserialize(TextReader tr)
    {
        var value = tr.ReadToEnd();

        // CalDateTime is defined as the Target type
        var parent = SerializationContext.Peek();

        // The associated object is an ICalendarObject of type CalendarProperty
        // that contains any timezone ("TZID" property) deserialized in a prior step
        var timeZoneId = (parent as ICalendarParameterCollectionContainer)?.Parameters.Get("TZID");

        if (CalDateTime.TryParse(value, timeZoneId, out var result))
        {
            return result;
        }

        return null;
    }

    public IReadOnlyList<CalendarParameter> GetParameters(object? value)
        => ParameterProviderHelper.GetCalDateTimeParameters(value);
}

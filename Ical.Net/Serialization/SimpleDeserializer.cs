//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System.Collections.Generic;
using System.IO;
using Ical.Net.CalendarComponents;

namespace Ical.Net.Serialization;

/// <summary>
/// Provides functionality to deserialize iCalendar data streams into
/// calendar component objects according to the RFC 5545 specification.
/// </summary>
/// <remarks>
/// The serializer parses unfolded iCalendar content lines and constructs a hierarchy of
/// calendar components and their properties. It supports standard iCalendar line folding
/// and parameter parsing rules.
/// <para/>
/// This class is thread-safe.
/// Use the static Default instance for typical deserialization scenarios.
/// </remarks>
public class SimpleDeserializer
{
    internal SimpleDeserializer()
    {
    }

    public static readonly SimpleDeserializer Default = new();

    public IEnumerable<ICalendarComponent> Deserialize(TextReader reader)
    {
        var strValue = reader.ReadToEnd();

        return Serialization2.CalendarSerializer.DeserializeCollection<CalendarComponent>(strValue);
    }
}

//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization.Converters;

namespace Ical.Net.Serialization;

internal static class ConverterMap
{
    internal static CalendarConverter GetConverter(string propertyName)
    {
        if (_converters.TryGetValue(propertyName, out var converter))
        {
            return converter;
        }

        // Property name is unknown. Treat value as a single text value.
        // The VALUE parameter will not change the parsed value type.
        return objectToStringConverter;
    }

    internal static CalendarComponent GetCalendarComponent(string name)
    {
        if (_components.TryGetValue(name, out var createInstance))
        {
            return createInstance();
        }

        return new CalendarComponent
        {
            Name = name.ToUpperInvariant()
        };
    }

    internal static readonly Dictionary<Type, Func<ICalendarDataType>> _propertyTypeCache = [];

    internal static ICalendarDataType CreateProperty(Type calendarPropertyType)
    {
        if (!typeof(ICalendarDataType).IsAssignableFrom(calendarPropertyType))
        {
            throw new SerializationException($"Converter property type must be a {nameof(ICalendarDataType)}");
        }

        if (!_propertyTypeCache.TryGetValue(calendarPropertyType, out var createInstance))
        {
            var newExp = Expression.New(calendarPropertyType);

            var lambdaExp = Expression.Lambda<Func<ICalendarDataType>>(newExp);

            createInstance = lambdaExp.Compile();

            _propertyTypeCache[calendarPropertyType] = createInstance;
        }

        return createInstance();
    }


    private static readonly AttachmentConverter attachmentConverter = new();

    private static readonly AttendeeConverter attendeeConverter= new();

    private static readonly StringConverter stringConverter = new();

    private static readonly ObjectToStringConverter objectToStringConverter= new();

    private static readonly Int32Converter int32Converter = new();

    private static readonly DateTimeConverter dateTimeConverter = new();

    private static readonly RecurrenceIdConverter recurrenceIdConverter = new();

    private static readonly GeoConverter geoConverter = new();

    private static readonly DurationConverter durationConverter = new();

    private static readonly RruleConverter rruleConverter = new();

    private static readonly TriggerConverter triggerConverter = new();

    private static readonly PeriodListConverter periodListConverter = new();
    private static readonly FreeBusyConverter freeBusyConverter = new();

    private static readonly UriConverter uriConverter = new();
    private static readonly OrganizerConverter organizerConverter = new();

    private static readonly UtcOffsetConverter utcOffsetConverter = new();
    private static readonly RequestStatusConverter requestStatusConverter = new();

    private static readonly StringListConverter stringListConverter = new();

#if NET8_0_OR_GREATER
    private static readonly FrozenDictionary<string, CalendarConverter> _converters = InitializeMap().ToFrozenDictionary();
#else
    private static readonly Dictionary<string, CalendarConverter> _converters = InitializeMap();
#endif

#if NET8_0_OR_GREATER
    private static readonly FrozenDictionary<string, Func<CalendarComponent>> _components = InitializeComponentMap().ToFrozenDictionary();
#else
    private static readonly Dictionary<string, Func<CalendarComponent>> _components = InitializeComponentMap();
#endif

    private static Dictionary<string, CalendarConverter> InitializeMap()
    {
        return new(StringComparer.OrdinalIgnoreCase)
        {
            { "ATTACH", attachmentConverter },
            { "ATTENDEE", attendeeConverter },
            { "CATEGORIES", stringListConverter},
            { "COMPLETED", dateTimeConverter},
            { "CREATED", dateTimeConverter},
            { "DTEND", dateTimeConverter},
            { "DTSTAMP", dateTimeConverter},
            { "DTSTART", dateTimeConverter},
            { "DUE", dateTimeConverter},
            { "DURATION", durationConverter},
            { "EXDATE", periodListConverter},
            { "FREEBUSY", freeBusyConverter},
            { "GEO", geoConverter},
            { "LAST-MODIFIED", dateTimeConverter},
            { "ORGANIZER", organizerConverter},
            { "PERCENT-COMPLETE", int32Converter},
            { "PRIORITY", int32Converter},
            { "RDATE", periodListConverter},
            { "RECURRENCE-ID", recurrenceIdConverter},
            { "REQUEST-STATUS", requestStatusConverter},
            { "REPEAT", int32Converter},
            { "RESOURCES", stringListConverter},
            { "RRULE", rruleConverter},
            { "SEQUENCE", int32Converter},
            { "STATUS", stringConverter},
            { "TRANSP", stringConverter},
            { TriggerRelation.Name, triggerConverter},
            { "TZOFFSETFROM", utcOffsetConverter},
            { "TZOFFSETTO", utcOffsetConverter},
            { "TZURL", uriConverter},
            { "URL", uriConverter},
        };
    }

    private static Dictionary<string, Func<CalendarComponent>> InitializeComponentMap()
    {
        return new(StringComparer.OrdinalIgnoreCase)
        {
            { Components.Alarm, static () => new Alarm() },
            { EventStatus.Name, static () => new CalendarEvent() },
            { Components.Freebusy, static () => new FreeBusy() },
            { JournalStatus.Name, static () => new Journal() },
            { Components.Timezone, static () => new VTimeZone() },
            { TodoStatus.Name, static () => new Todo() },
            { Components.Calendar, static () => new Calendar() },
            { Components.Daylight, static () => new VTimeZoneInfo(Components.Daylight) },
            { Components.Standard, static () => new VTimeZoneInfo(Components.Standard) },
        };
    }
}

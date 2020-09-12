using System;
using System.Collections.Generic;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization
{
    internal sealed class DataTypeMapper
    {
        private delegate Type TypeResolverDelegate(object context);

        private readonly IDictionary<string, PropertyMapping> _propertyMap = new Dictionary<string, PropertyMapping>(StringComparer.OrdinalIgnoreCase)
        {
            { AlarmAction.Name, new PropertyMapping(typeof(AlarmAction), false) },
            { "ATTACH", new PropertyMapping(typeof(Attachment), false) },
            { "ATTENDEE", new PropertyMapping(typeof(Attendee), false) },
            { "CATEGORIES", new PropertyMapping(typeof(string), true) },
            { "COMMENT", new PropertyMapping(typeof(string), false) },
            { "COMPLETED", new PropertyMapping(typeof(IDateTime), false) },
            { "CONTACT", new PropertyMapping(typeof(string), false) },
            { "CREATED", new PropertyMapping(typeof(IDateTime), false) },
            { "DTEND", new PropertyMapping(typeof(IDateTime), false) },
            { "DTSTAMP", new PropertyMapping(typeof(IDateTime), false) },
            { "DTSTART", new PropertyMapping(typeof(IDateTime), false) },
            { "DUE", new PropertyMapping(typeof(IDateTime), false) },
            { "DURATION", new PropertyMapping(typeof(TimeSpan), false) },
            { "EXDATE", new PropertyMapping(typeof(PeriodList), false) },
            { "EXRULE", new PropertyMapping(typeof(RecurrencePattern), false) },
            { "FREEBUSY", new PropertyMapping(typeof(FreeBusyEntry), true) },
            { "GEO", new PropertyMapping(typeof(GeographicLocation), false) },
            { "LAST-MODIFIED", new PropertyMapping(typeof(IDateTime), false) },
            { "ORGANIZER", new PropertyMapping(typeof(Organizer), false) },
            { "PERCENT-COMPLETE", new PropertyMapping(typeof(int), false) },
            { "PRIORITY", new PropertyMapping(typeof(int), false) },
            { "RDATE", new PropertyMapping(typeof(PeriodList), false) },
            { "RECURRENCE-ID", new PropertyMapping(typeof(IDateTime), false) },
            { "RELATED-TO", new PropertyMapping(typeof(string), false) },
            { "REQUEST-STATUS", new PropertyMapping(typeof(RequestStatus), false) },
            { "REPEAT", new PropertyMapping(typeof(int), false) },
            { "RESOURCES", new PropertyMapping(typeof(string), true) },
            { "RRULE", new PropertyMapping(typeof(RecurrencePattern), false) },
            { "SEQUENCE", new PropertyMapping(typeof(int), false) },
            { "STATUS", new PropertyMapping(ResolveStatusProperty, false) },
            { "TRANSP", new PropertyMapping(typeof(TransparencyType), false) },
            { TriggerRelation.Name, new PropertyMapping(typeof(Trigger), false) },
            { "TZNAME", new PropertyMapping(typeof(string), false) },
            { "TZOFFSETFROM", new PropertyMapping(typeof(UtcOffset), false) },
            { "TZOFFSETTO", new PropertyMapping(typeof(UtcOffset), false) },
            { "TZURL", new PropertyMapping(typeof(Uri), false) },
            { "URL", new PropertyMapping(typeof(Uri), false) }
        };

        private static Type ResolveStatusProperty(object context)
        {
            var obj = context as ICalendarObject;
            if (obj == null)
            {
                return null;
            }

            switch (obj.Parent)
            {
                case CalendarEvent _:
                    return typeof (EventStatus);
                case Todo _:
                    return typeof (TodoStatus);
                case Journal _:
                    return typeof (JournalStatus);
            }

            return null;
        }

        public bool IsPropertyAllowMultipleValues(object obj)
        {
            var property = obj as ICalendarProperty;
            if (string.IsNullOrWhiteSpace(property?.Name))
            {
                return false;
            }

            _propertyMap.TryGetValue(property.Name, out var propertyMapping);
            return propertyMapping?.AllowsMultipleValues ?? false;
        }

        public Type GetPropertyMapping(object obj)
        {
            var property = obj as ICalendarProperty;
            if (property?.Name == null)
            {
                return null;
            }

            if (!_propertyMap.TryGetValue(property.Name, out var propertyMapping))
            {
                return null;
            }

            return propertyMapping.Resolver == null
                ? propertyMapping.ObjectType
                : propertyMapping.Resolver(property);
        }

        private sealed class PropertyMapping
        {
            public PropertyMapping(Type objectType, bool allowsMultipleValues)
            {
                ObjectType = objectType;
                Resolver = null;
                AllowsMultipleValues = allowsMultipleValues;
            }

            public PropertyMapping(TypeResolverDelegate resolver, bool allowsMultipleValues)
            {
                ObjectType = null;
                Resolver = resolver;
                AllowsMultipleValues = allowsMultipleValues;
            }

            public Type ObjectType { get; }
            public TypeResolverDelegate Resolver { get; }
            public bool AllowsMultipleValues { get; }
        }
    }
}
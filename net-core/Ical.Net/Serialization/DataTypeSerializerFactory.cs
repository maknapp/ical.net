using System;
using System.Reflection;
using Ical.Net.DataTypes;
using Ical.Net.Serialization.DataTypes;

namespace Ical.Net.Serialization
{
    public sealed class DataTypeSerializerFactory : ISerializerFactory
    {
        /// <summary>
        /// Returns a serializer that can be used to serialize and object
        /// of type <paramref name="objectType"/>.
        /// <note>
        ///     TODO: Add support for caching.
        /// </note>
        /// </summary>
        /// <param name="objectType">The type of object to be serialized.</param>
        /// <param name="ctx">The serialization context.</param>
        public ISerializer Build(Type objectType, SerializationContext ctx)
        {
            if (objectType == null) return null;
            
            if (typeof (Attachment).IsAssignableFrom(objectType))
            {
                return new AttachmentSerializer(ctx);
            }

            if (typeof (Attendee).IsAssignableFrom(objectType))
            {
                return new AttendeeSerializer(ctx);
            }

            if (typeof (IDateTime).IsAssignableFrom(objectType))
            {
                return new DateTimeSerializer(ctx);
            }

            if (typeof (FreeBusyEntry).IsAssignableFrom(objectType))
            {
                return new FreeBusyEntrySerializer(ctx);
            }

            if (typeof (GeographicLocation).IsAssignableFrom(objectType))
            {
                return new GeographicLocationSerializer(ctx);
            }

            if (typeof (Organizer).IsAssignableFrom(objectType))
            {
                return new OrganizerSerializer(ctx);
            }

            if (typeof (Period).IsAssignableFrom(objectType))
            {
                return new PeriodSerializer(ctx);
            }

            if (typeof (PeriodList).IsAssignableFrom(objectType))
            {
                return new PeriodListSerializer(ctx);
            }

            if (typeof (RecurrencePattern).IsAssignableFrom(objectType))
            {
                return new RecurrencePatternSerializer(ctx);
            }

            if (typeof (RequestStatus).IsAssignableFrom(objectType))
            {
                return new RequestStatusSerializer(ctx);
            }

            if (typeof (StatusCode).IsAssignableFrom(objectType))
            {
                return new StatusCodeSerializer(ctx);
            }

            if (typeof (Trigger).IsAssignableFrom(objectType))
            {
                return new TriggerSerializer(ctx);
            }

            if (typeof (UtcOffset).IsAssignableFrom(objectType))
            {
                return new UtcOffsetSerializer(ctx);
            }

            if (typeof (WeekDay).IsAssignableFrom(objectType))
            {
                return new WeekDaySerializer(ctx);
            }

            // Default to a string serializer, which simply calls
            // ToString() on the value to serialize it.
            return new StringSerializer(ctx);
        }
    }
}
using System;
using System.Reflection;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization.DataTypes;

namespace Ical.Net.Serialization
{
    public sealed class SerializerFactory : ISerializerFactory
    {
        private readonly ISerializerFactory _serializerFactory;

        public SerializerFactory()
        {
            _serializerFactory = new DataTypeSerializerFactory();
        }

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
            if (objectType == null)
            {
                return null;
            }

            if (typeof (Calendar).IsAssignableFrom(objectType))
            {
                return new CalendarSerializer(ctx);
            }

            if (typeof (ICalendarComponent).IsAssignableFrom(objectType))
            {
                return typeof (CalendarEvent).IsAssignableFrom(objectType)
                    ? new EventSerializer(ctx)
                    : new ComponentSerializer(ctx);
            }

            if (typeof (ICalendarProperty).IsAssignableFrom(objectType))
            {
                return new PropertySerializer(ctx);
            }

            if (typeof (CalendarParameter).IsAssignableFrom(objectType))
            {
                return new ParameterSerializer(ctx);
            }

            if (typeof (string).IsAssignableFrom(objectType))
            {
                return new StringSerializer(ctx);
            }

            if (objectType.GetTypeInfo().IsEnum)
            {
                return new EnumSerializer(objectType, ctx);
            }

            if (typeof (TimeSpan).IsAssignableFrom(objectType))
            {
                return new TimeSpanSerializer(ctx);
            }

            if (typeof (int).IsAssignableFrom(objectType))
            {
                return new IntegerSerializer(ctx);
            }

            if (typeof (Uri).IsAssignableFrom(objectType))
            {
                return new UriSerializer(ctx);
            }

            if (typeof (ICalendarDataType).IsAssignableFrom(objectType))
            {
                return _serializerFactory.Build(objectType, ctx);
            }

            // Default to a string serializer, which simply calls
            // ToString() on the value to serialize it.
            return new StringSerializer(ctx);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ical.Net.CalendarComponents;
using Ical.Net.Utilities;

namespace Ical.Net.Serialization
{
    public class ComponentSerializer : IStringSerializer
    {
        protected ComponentSerializer() : this(SerializationContext.Default) { }

        public ComponentSerializer(SerializationContext ctx)
        {
            SerializationContext = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        private SerializationContext SerializationContext { get; }

        public virtual Type TargetType => typeof(CalendarComponent);

        // TODO: Utilise PropertySorter
        public virtual IComparer<ICalendarProperty> PropertySorter => new PropertyAlphabetizer();

        public virtual string Serialize(object obj)
        {
            if (!(obj is ICalendarComponent c))
            {
                return null;
            }

            var sb = new StringBuilder();
            var upperName = c.Name.ToUpperInvariant();
            SerializeComponentStart(sb, upperName);

            // Get a serializer factory
            var sf = SerializationContext.GetService<ISerializerFactory>();

            // Sort the calendar properties in alphabetical order before serializing them!
            var properties = c.Properties.OrderBy(p => p.Name);

            SerializeProperties(properties, sf, sb);
            SerializeChildObjects(c, sf, sb);

            SerializeComponentEnd(sb, upperName);

            return sb.ToString();
        }

        private static void SerializeComponentStart(StringBuilder builder, string name)
        {
            builder.Append(TextUtil.FoldLines($"BEGIN:{name}"));
        }

        private static void SerializeComponentEnd(StringBuilder builder, string name)
        {
            builder.Append(TextUtil.FoldLines($"END:{name}"));
        }

        private void SerializeProperties(
            IEnumerable<ICalendarProperty> properties, ISerializerFactory serializerFactory, StringBuilder builder)
        {
            foreach (var property in properties)
            {
                // Get a serializer for each property.
                IStringSerializer serializer = serializerFactory.Build(property.GetType(), SerializationContext);
                builder.Append(serializer.Serialize(property));
            }
        }

        private void SerializeChildObjects(
            ICalendarComponent component, ISerializerFactory serializerFactory, StringBuilder builder)
        {
            foreach (var child in component.Children)
            {
                // Get a serializer for each child object.
                IStringSerializer serializer = serializerFactory.Build(child.GetType(), SerializationContext);
                builder.Append(serializer.Serialize(child));
            }
        }

        public virtual object Deserialize(string value) => null;

        private class PropertyAlphabetizer : IComparer<ICalendarProperty>
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
                return y == null
                    ? 1
                    : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}

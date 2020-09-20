using System;
using System.Collections.Generic;
using System.IO;
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

        public virtual IComparer<ICalendarProperty> PropertySorter => new PropertyAlphabetizer();

        public virtual string Serialize(object obj)
        {
            if (!(obj is ICalendarComponent c))
            {
                return null;
            }

            var sb = new StringBuilder();
            var upperName = c.Name.ToUpperInvariant();
            sb.Append(TextUtil.FoldLines($"BEGIN:{upperName}"));

            // Get a serializer factory
            var sf = SerializationContext.GetService<ISerializerFactory>();

            // Sort the calendar properties in alphabetical order before serializing them!
            var properties = c.Properties.OrderBy(p => p.Name).ToList();

            // Serialize properties
            foreach (var p in properties)
            {
                // Get a serializer for each property.
                var serializer = sf.Build(p.GetType(), SerializationContext) as IStringSerializer;
                sb.Append(serializer.Serialize(p));
            }

            // Serialize child objects
            foreach (var child in c.Children)
            {
                // Get a serializer for each child object.
                var serializer = sf.Build(child.GetType(), SerializationContext) as IStringSerializer;
                sb.Append(serializer.Serialize(child));
            }

            sb.Append(TextUtil.FoldLines($"END:{upperName}"));
            return sb.ToString();
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

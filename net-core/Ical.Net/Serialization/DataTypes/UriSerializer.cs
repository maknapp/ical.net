using System;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes
{
    public sealed class UriSerializer : DataTypeSerializer
    {
        public UriSerializer(SerializationContext ctx) : base(ctx) {}

        public override Type TargetType => typeof (string);

        public override string Serialize(object obj)
        {
            if (!(obj is Uri))
            {
                return null;
            }

            var uri = (Uri) obj;

            if (SerializationContext.Peek() is ICalendarObject co)
            {
                var dt = new EncodableDataType
                {
                    AssociatedObject = co
                };
                return Encode(dt, uri.OriginalString);
            }
            return uri.OriginalString;
        }

        public override object Deserialize(string value)
        {
            if (SerializationContext.Peek() is ICalendarObject co)
            {
                var dt = new EncodableDataType
                {
                    AssociatedObject = co
                };
                value = Decode(dt, value);
            }

            try
            {
                var uri = new Uri(value);
                return uri;
            }
            catch { }
            return null;
        }
    }
}

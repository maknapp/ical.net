using System;
using Ical.Net.Serialization.DataTypes;

namespace Ical.Net.Serialization
{
    public sealed class DataMapSerializer : IStringSerializer
    {
        public DataMapSerializer() : this(SerializationContext.Default) { }

        public DataMapSerializer(SerializationContext ctx)
        {
            SerializationContext = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        private SerializationContext SerializationContext { get; }

        public Type TargetType
        {
            get
            {
                IStringSerializer serializer = GetMappedSerializer();
                return serializer?.TargetType;
            }
        }

        public string Serialize(object obj)
        {
            var serializer = GetMappedSerializer();
            return serializer?.Serialize(obj);
        }

        public object Deserialize(string value)
        {
            IStringSerializer serializer = GetMappedSerializer();
            if (serializer == null)
            {
                return null;
            }

            var returnValue = serializer.Deserialize(value);

            // Default to returning the string representation of the value
            // if the value wasn't formatted correctly.
            // FIXME: should this be a try/catch?  Should serializers be throwing
            // an InvalidFormatException?  This may have some performance issues
            // as try/catch is much slower than other means.
            return returnValue ?? value;
        }

        private IStringSerializer GetMappedSerializer()
        {
            var sf = SerializationContext.GetService<ISerializerFactory>();
            var mapper = SerializationContext.GetService<DataTypeMapper>();
            if (sf == null || mapper == null)
            {
                return null;
            }

            var obj = SerializationContext.Peek();

            // Get the data type for this object
            var type = mapper.GetPropertyMapping(obj);

            return type == null
                ? new StringSerializer(SerializationContext)
                : sf.Build(type, SerializationContext) as IStringSerializer;
        }
    }
}

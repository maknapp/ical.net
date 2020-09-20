using System;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes
{
    public abstract class DataTypeSerializer : IStringSerializer
    {
        protected DataTypeSerializer(SerializationContext ctx)
        {
            SerializationContext = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        protected SerializationContext SerializationContext { get; }

        public abstract Type TargetType { get; }

        public abstract string Serialize(object obj);
        public abstract object Deserialize(string value);

        protected T CreateAndAssociate<T>() where T: class, ICalendarDataType
        {
            // Create an instance of the object
            var dataType = Activator.CreateInstance(TargetType) as T;
            if (dataType == null)
            {
                return default;
            }

            if (SerializationContext.Peek() is ICalendarObject associatedObject)
            {
                dataType.AssociatedObject = associatedObject;
            }

            return dataType;
        }

        protected string Encode(IEncodableDataType dt, string value)
        {
            if (value == null)
            {
                return null;
            }

            if (dt?.Encoding == null)
            {
                return value;
            }

            // Return the value in the current encoding
            var encodingStack = SerializationContext.GetService<EncodingStack>();
            return Encode(dt, encodingStack.Current.GetBytes(value));
        }

        protected string Encode(IEncodableDataType dt, byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            if (dt?.Encoding == null)
            {
                // Default to the current encoding
                var encodingStack = SerializationContext.GetService<EncodingStack>();
                return encodingStack.Current.GetString(data);
            }

            return EncodingProvider.Encode(dt.Encoding, data);
        }

        protected string Decode(IEncodableDataType dt, string value)
        {
            if (dt?.Encoding == null)
            {
                return value;
            }

            byte[] data = DecodeData(dt, value);
            if (data == null)
            {
                return null;
            }

            // Default to the current encoding
            var encodingStack = SerializationContext.GetService<EncodingStack>();
            return encodingStack.Current.GetString(data);
        }

        protected byte[] DecodeData(IEncodableDataType dt, string value)
        {
            if (value == null)
            {
                return null;
            }

            if (dt?.Encoding == null)
            {
                // Default to the current encoding
                var encodingStack = SerializationContext.GetService<EncodingStack>();
                return encodingStack.Current.GetBytes(value);
            }

            return EncodingProvider.DecodeData(dt.Encoding, value);
        }
    }
}

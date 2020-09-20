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

        protected ICalendarDataType CreateAndAssociate()
        {
            // Create an instance of the object
            if (!(Activator.CreateInstance(TargetType) is ICalendarDataType dt))
            {
                return null;
            }

            if (SerializationContext.Peek() is ICalendarObject associatedObject)
            {
                dt.AssociatedObject = associatedObject;
            }

            return dt;
        }
    }
}

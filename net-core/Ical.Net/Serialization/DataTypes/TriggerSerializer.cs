using System;
using System.IO;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes
{
    public sealed class TriggerSerializer : StringSerializer
    {
        public TriggerSerializer() { }

        public TriggerSerializer(SerializationContext ctx) : base(ctx) { }

        public override Type TargetType => typeof (Trigger);

        public override string Serialize(object obj)
        {
            try
            {
                if (!(obj is Trigger t))
                {
                    return null;
                }

                // Push the trigger onto the serialization stack
                SerializationContext.Push(t);
                try
                {
                    var factory = SerializationContext.GetService<ISerializerFactory>();
                    if (factory == null)
                    {
                        return null;
                    }

                    var valueType = t.GetValueType() ?? typeof(TimeSpan);
                    if (!(factory.Build(valueType, SerializationContext) is IStringSerializer serializer))
                    {
                        return null;
                    }

                    var value = valueType == typeof(IDateTime)
                        ? t.DateTime
                        : (object) t.Duration;
                    return serializer.Serialize(value);
                }
                finally
                {
                    // Pop the trigger off the serialization stack
                    SerializationContext.Pop();
                }
            }
            catch
            {
                return null;
            }
        }

        public override object Deserialize(string value)
        {
            if (!(CreateAndAssociate() is Trigger trigger))
            {
                return null;
            }

            // Push the trigger onto the serialization stack
            SerializationContext.Push(trigger);
            try
            {
                // Decode the value as needed
                value = Decode(trigger, value);

                // Set the trigger relation
                if (trigger.Parameters.ContainsKey("RELATED") && trigger.Parameters.Get("RELATED").Equals("END"))
                {
                    trigger.Related = TriggerRelation.End;
                }

                var factory = SerializationContext.GetService<ISerializerFactory>();
                if (factory == null)
                {
                    return null;
                }

                var valueType = trigger.GetValueType() ?? typeof(TimeSpan);
                var serializer = factory.Build(valueType, SerializationContext) as IStringSerializer;
                var obj = serializer?.Deserialize(value);
                switch (obj)
                {
                    case null:
                        return null;
                    case IDateTime _:
                        trigger.DateTime = (IDateTime) obj;
                        break;
                    default:
                        trigger.Duration = (TimeSpan) obj;
                        break;
                }

                return trigger;
            }
            finally
            {
                // Pop the trigger off the serialization stack
                SerializationContext.Pop();
            }
        }
    }
}

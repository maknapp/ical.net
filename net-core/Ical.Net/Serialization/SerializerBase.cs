using System;
using System.IO;
using System.Text;

namespace Ical.Net.Serialization
{
    public abstract class SerializerBase : IStringSerializer
    {
        protected SerializerBase(SerializationContext ctx)
        {
            SerializationContext = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        protected SerializationContext SerializationContext { get; }

        public abstract Type TargetType { get; }

        public abstract string Serialize(object obj);
        public abstract object Deserialize(string value);
        
        protected T GetService<T>()
        {
            return SerializationContext != null ? SerializationContext.GetService<T>() : default;
        }
    }
}

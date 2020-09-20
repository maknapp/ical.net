using System;

namespace Ical.Net.Serialization
{
    public interface ISerializerFactory
    {
        IStringSerializer Build(Type objectType, SerializationContext ctx);
    }
}

using System;

namespace Ical.Net.Serialization
{
    public interface IStringSerializer
    {
        Type TargetType { get; }

        string Serialize(object obj);

        object Deserialize(string value);
    }
}

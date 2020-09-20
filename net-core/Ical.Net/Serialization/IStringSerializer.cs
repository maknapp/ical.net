using System.IO;

namespace Ical.Net.Serialization
{
    public interface IStringSerializer : ISerializer
    {
        string Serialize(object obj);
        object Deserialize(TextReader reader);
    }
}

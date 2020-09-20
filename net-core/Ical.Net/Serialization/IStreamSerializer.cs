using System.IO;
using System.Text;

namespace Ical.Net.Serialization
{
    public interface IStreamSerializer 
    {
        void Serialize(object obj, Stream stream, Encoding encoding);
        object Deserialize(Stream stream, Encoding encoding);
    }
}

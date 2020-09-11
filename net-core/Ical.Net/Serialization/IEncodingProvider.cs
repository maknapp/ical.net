namespace Ical.Net.Serialization
{
    internal interface IEncodingProvider
    {
        string Encode(string encoding, byte[] data);
        byte[] DecodeData(string encoding, string value);
    }
}
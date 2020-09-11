using System;

namespace Ical.Net.Serialization
{
    internal sealed class EncodingProvider
    {
        public static string Encode(string encoding, byte[] data)
        {
            if (encoding == null || data == null)
            {
                return null;
            }
            
            switch (encoding.ToUpper())
            {
                // TODO: Consider folding the output lines.
                //      Base64FormattingOptions.InsertLineBreaks might be acceptable but we need to consider the line
                //      breaks.
                case "BASE64":
                    return Convert.ToBase64String(data);

                default:
                    return null;
            }
        }

        public static byte[] DecodeData(string encoding, string value)
        {
            if (encoding == null || value == null)
            {
                return null;
            }

            switch (encoding.ToUpper())
            {

                case "BASE64":
                    return Convert.FromBase64String(value);
                default:
                    return null;
            }
        }
    }
}
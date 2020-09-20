using System;
using System.Text;

namespace Ical.Net.Serialization
{
    public sealed class ParameterSerializer : IStringSerializer
    {
        public ParameterSerializer(SerializationContext ctx) {}

        public Type TargetType => typeof (CalendarParameter);

        public string Serialize(object obj)
        {
            if (!(obj is CalendarParameter p))
            {
                return null;
            }

            var builder = new StringBuilder();
            builder.Append(p.Name + "=");

            // "Section 3.2:  Property parameter values MUST NOT contain the DQUOTE character."
            // Therefore, let's strip any double quotes from the value.
            var values = string.Join(",", p.Values).Replace("\"", string.Empty);

            // Surround the parameter value with double quotes, if the value
            // contains any problematic characters.
            if (values.IndexOfAny(new[] { ';', ':', ',' }) >= 0)
            {
                values = "\"" + values + "\"";
            }
            builder.Append(values);
            return builder.ToString();
        }

        public object Deserialize(string value) => null;
    }
}

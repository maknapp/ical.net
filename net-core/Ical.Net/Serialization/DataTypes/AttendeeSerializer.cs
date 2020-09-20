using System;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes
{
    public sealed class AttendeeSerializer : StringSerializer
    {
        public AttendeeSerializer() { }

        public AttendeeSerializer(SerializationContext ctx) : base(ctx) { }

        public override Type TargetType => typeof (Attendee);

        public override string Serialize(object obj)
        {
            var a = obj as Attendee;
            return a?.Value == null
                ? null
                : Encode(a, a.Value.OriginalString);
        }

        public override object Deserialize(string value)
        {
            try
            {
                var attendee = CreateAndAssociate() as Attendee;
                var uriString = Unescape(Decode(attendee, value));

                // Prepend "mailto:" if necessary
                if (!uriString.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                {
                    uriString = "mailto:" + uriString;
                }

                attendee.Value = new Uri(uriString);
                return attendee;
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}

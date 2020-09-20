using System;
using System.IO;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes
{
    public sealed class OrganizerSerializer : StringSerializer
    {
        public OrganizerSerializer() { }

        public OrganizerSerializer(SerializationContext ctx) : base(ctx) { }

        public override Type TargetType => typeof (Organizer);

        public override string Serialize(object obj)
        {
            try
            {
                var o = obj as Organizer;
                return o?.Value == null
                    ? null
                    : Encode(o, Escape(o.Value.OriginalString));
            }
            catch
            {
                return null;
            }
        }

        public override object Deserialize(string value)
        {
            var organizer = CreateAndAssociate() as Organizer;
            try
            {
                if (organizer != null)
                {
                    var uriString = Unescape(Decode(organizer, value));

                    // Prepend "mailto:" if necessary
                    if (!uriString.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                    {
                        uriString = "mailto:" + uriString;
                    }

                    organizer.Value = new Uri(uriString);
                }
            }
            catch
            {
                // TODO: Ignore exceptions selectively
            }

            return organizer;
        }
    }
}

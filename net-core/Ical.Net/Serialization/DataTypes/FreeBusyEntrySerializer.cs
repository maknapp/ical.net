using System;
using System.IO;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes
{
    public sealed class FreeBusyEntrySerializer : PeriodSerializer
    {
        public FreeBusyEntrySerializer() { }

        public FreeBusyEntrySerializer(SerializationContext ctx) : base(ctx) { }

        public override Type TargetType => typeof (FreeBusyEntry);

        public override string Serialize(object obj)
        {
            var entry = obj as FreeBusyEntry;
            if (entry == null)
            {
                return base.Serialize(obj);
            }

            switch (entry.Status)
            {
                case FreeBusyStatus.Busy:
                    entry.Parameters.Remove("FBTYPE");
                    break;
                case FreeBusyStatus.BusyTentative:
                    entry.Parameters.Set("FBTYPE", "BUSY-TENTATIVE");
                    break;
                case FreeBusyStatus.BusyUnavailable:
                    entry.Parameters.Set("FBTYPE", "BUSY-UNAVAILABLE");
                    break;
                case FreeBusyStatus.Free:
                    entry.Parameters.Set("FBTYPE", "FREE");
                    break;
            }

            return base.Serialize(obj);
        }

        public override object Deserialize(string value)
        {
            var entry = base.Deserialize(value) as FreeBusyEntry;
            if (entry == null)
            {
                return null;
            }

            if (!entry.Parameters.ContainsKey("FBTYPE"))
            {
                return entry;
            }

            var type = entry.Parameters.Get("FBTYPE");
            if (type == null)
            {
                return entry;
            }

            switch (type.ToUpperInvariant())
            {
                case "FREE":
                    entry.Status = FreeBusyStatus.Free;
                    break;
                case "BUSY":
                    entry.Status = FreeBusyStatus.Busy;
                    break;
                case "BUSY-UNAVAILABLE":
                    entry.Status = FreeBusyStatus.BusyUnavailable;
                    break;
                case "BUSY-TENTATIVE":
                    entry.Status = FreeBusyStatus.BusyTentative;
                    break;
            }

            return entry;
        }
    }
}

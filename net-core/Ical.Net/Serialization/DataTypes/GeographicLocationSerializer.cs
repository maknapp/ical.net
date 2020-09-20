using System;
using System.Globalization;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes
{
    public sealed class GeographicLocationSerializer : DataTypeSerializer
    {
        public GeographicLocationSerializer() : base(SerializationContext.Default) { }

        public GeographicLocationSerializer(SerializationContext ctx) : base(ctx) { }

        public override Type TargetType => typeof (GeographicLocation);

        public override string Serialize(object obj)
        {
            var g = obj as GeographicLocation;
            if (g == null)
            {
                return null;
            }

            var value = g.Latitude.ToString("0.000000", CultureInfo.InvariantCulture.NumberFormat) + ";"
                + g.Longitude.ToString("0.000000", CultureInfo.InvariantCulture.NumberFormat);
            return Encode(g, value);
        }

        public override object Deserialize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var geoLocation = CreateAndAssociate<GeographicLocation>();
            if (geoLocation == null)
            {
                return null;
            }

            // Decode the value, if necessary!
            value = Decode(geoLocation, value);

            var values = value.Split(new [] {';'}, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != 2)
            {
                return null;
            }

            double.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var lat);
            double.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var lon);
            geoLocation.Latitude = lat;
            geoLocation.Longitude = lon;

            return geoLocation;
        }
    }
}

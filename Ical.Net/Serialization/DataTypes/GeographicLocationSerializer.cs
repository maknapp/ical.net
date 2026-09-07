//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.IO;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes;

public class GeographicLocationSerializer : EncodableDataTypeSerializer
{
    public GeographicLocationSerializer() { }

    public GeographicLocationSerializer(SerializationContext ctx) : base(ctx) { }

    public override Type TargetType => typeof(GeographicLocation);

    public override string? SerializeToString(object? obj)
    {
        if (obj is not GeographicLocation location)
        {
            return null;
        }

        return location.ToString();
    }

    public GeographicLocation? Deserialize(string value)
    {
        if (GeographicLocation.TryParse(value, out var geo))
        {
            return geo;
        }

        return null;
    }

    public override object? Deserialize(TextReader tr) => Deserialize(tr.ReadToEnd());
}

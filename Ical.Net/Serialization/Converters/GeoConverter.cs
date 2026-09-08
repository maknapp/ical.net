//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace Ical.Net.Serialization.Converters;

internal class GeoConverter : CalendarPropertyConverter<GeographicLocation>
{
    public override GeographicLocation? Read(CalendarReader reader, IParameterCollection parameters)
    {
        var strValue = reader.GetTextValue();

        if (GeographicLocation.TryParse(strValue, out var geo))
        {
            return geo;
        }

        return null;
    }

    public override void Write(CalendarWriter writer, GeographicLocation value)
    {
        writer.WriteValue(value.ToString());
    }
}

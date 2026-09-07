//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.IO;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes;

public class PeriodListSerializer : EncodableDataTypeSerializer
{
    public PeriodListSerializer() { }

    public PeriodListSerializer(SerializationContext ctx) : base(ctx) { }

    public override Type TargetType => typeof(PeriodList);

    public override string? SerializeToString(object? obj)
    {
        if (obj is not PeriodList periodList)
        {
            return null;
        }

        return periodList.ToString();
    }

    public override object? Deserialize(TextReader tr)
    {
        var value = tr.ReadToEnd();

        PeriodList periodList = [];

        var values = value.Split(',');
        foreach (var v in values)
        {
            if (CalDateTime.TryParse(v, out var duration))
            {
                periodList.Add(duration);
            }
            else if (Period.TryParse(v, null, out var period))
            {
                periodList.Add(period);
            }
        }

        return periodList;
    }
}

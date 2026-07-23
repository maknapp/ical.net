//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.IO;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes;

public class PeriodSerializer : EncodableDataTypeSerializer
{
    public PeriodSerializer() { }

    public PeriodSerializer(SerializationContext ctx) : base(ctx) { }

    public override Type TargetType => typeof(Period);

    public override string? SerializeToString(object? obj)
    {
        if (obj is not Period p)
        {
            return null;
        }

        return p.ToBasicIso();
    }

    public override object? Deserialize(TextReader tr)
    {
        var value = tr.ReadToEnd();

        if (Period.TryParse(value, null, out var period))
        {
            return period;
        }

        return null;
    }
}

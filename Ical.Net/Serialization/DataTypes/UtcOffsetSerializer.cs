//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.IO;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes;

public class UtcOffsetSerializer : EncodableDataTypeSerializer
{
    public UtcOffsetSerializer() { }

    public UtcOffsetSerializer(SerializationContext ctx) : base(ctx) { }

    public override Type TargetType => typeof(UtcOffset);

    public override string? SerializeToString(object? obj)
    {
        if (obj is not UtcOffset offset) return null;

        return offset.ToString();
    }

    public override object? Deserialize(TextReader tr)
    {
        var offsetString = tr.ReadToEnd();

        if (UtcOffset.TryParse(offsetString, out var utcOffset))
        {
            return utcOffset;
        }

        return null;
    }
}

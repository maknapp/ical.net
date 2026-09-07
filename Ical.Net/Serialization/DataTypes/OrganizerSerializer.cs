//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.IO;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes;

public class OrganizerSerializer : StringSerializer
{
    public OrganizerSerializer() { }

    public OrganizerSerializer(SerializationContext ctx) : base(ctx) { }

    public override Type TargetType => typeof(Organizer);

    public override string? SerializeToString(object? obj) => (obj as Organizer)?.Value?.OriginalString;

    public override object? Deserialize(TextReader? tr)
    {
        if (tr == null) return null;

        var value = tr.ReadToEnd();

        if (Organizer.TryParse(value, out var organizer))
        {
            return organizer;
        }

        return null;
    }
}

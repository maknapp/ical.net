//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.IO;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization.DataTypes;

public class RecurrenceRuleSerializer : EncodableDataTypeSerializer
{
    public RecurrenceRuleSerializer() { }

    public RecurrenceRuleSerializer(SerializationContext ctx) : base(ctx) { }

    public override Type TargetType => typeof(RecurrenceRule);

    public override string? SerializeToString(object? obj)
    {
        if (obj is not RecurrenceRule recur)
        {
            return null;
        }

        return recur.ToString();
    }

    /// <summary>
    /// Deserializes an RRULE value string into an <see cref="RecurrenceRule"/> object.
    /// <para/>
    /// RFC5545, section 3.3.10:
    /// The RRULE value type is a structured value consisting of a
    /// list of one or more recurrence grammar parts. Each rule part is
    /// defined by a NAME=VALUE pair. The rule parts are separated from
    /// each other by the SEMICOLON character. The rule parts are not
    /// ordered in any particular sequence. Individual rule parts MUST
    /// only be specified once. Compliant applications MUST accept rule
    /// parts ordered in any sequence.
    /// </summary>
    /// <param name="tr"></param>
    /// <returns>An <see cref="RecurrenceRule"/> object or <see langword="null"/> for invalid input.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public override object? Deserialize(TextReader tr)
    {
        var value = tr.ReadToEnd();

        return RecurrenceRule.Parse(value);
    }
}

//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using Ical.Net.DataTypes;

namespace Ical.Net.Serialization2.Converters;

internal class RruleConverter : CalendarPropertyConverter<RecurrenceRule>
{
    public override RecurrenceRule? Read(CalendarReader reader, IParameterCollection parameters)
    {
        var value = reader.GetRecurValue();

        return RecurrenceRule.Parse(value);
    }

    public override void Write(CalendarWriter writer, RecurrenceRule recur)
    {
        writer.WriteValueRaw(recur.ToString());
    }
}

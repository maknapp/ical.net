//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ical.Net.Serialization2.Converters;

internal class StringListConverter : CalendarPropertyConverter<IEnumerable<object>>
{
    public override bool IsListValue => true;

    public override IEnumerable<object> Read(CalendarReader reader, IParameterCollection parameters)
    {
        while (reader.TryGetTextValue(out var textValue))
        {
            yield return textValue;
        }
    }

    public override void Write(CalendarWriter writer, IEnumerable<object> values)
    {
        foreach (var value in values)
        {
            writer.WriteValue((string)value);
        }
    }
}

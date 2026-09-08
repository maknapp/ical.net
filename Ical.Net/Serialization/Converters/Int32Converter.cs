//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Globalization;
using Ical.Net.Serialization;

namespace Ical.Net.Serialization.Converters;

internal class Int32Converter : CalendarPropertyConverter<int>
{
    public override int Read(CalendarReader reader, IParameterCollection parameters) => Convert.ToInt32(reader.GetTextValue(), CultureInfo.InvariantCulture);
    public override void Write(CalendarWriter writer, int value) => writer.WriteValue(value);
}

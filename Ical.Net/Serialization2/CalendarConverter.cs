//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization2;

public abstract class CalendarConverter
{
    public abstract Type Type { get; }

    public abstract bool IsListValue { get; }

    public abstract bool CanConvert(Type typeToConvert);

    public abstract bool IsValueBase64(ICalendarParameterCollectionContainer container);

    internal abstract object? ReadObject(CalendarReader reader, IParameterCollection parameters);

    internal abstract void WriteObject(CalendarWriter writer, object value);

    internal abstract void WriteObjectParameters(CalendarWriter writer, ICalendarParameterCollectionContainer container);
}

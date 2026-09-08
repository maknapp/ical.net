//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace Ical.Net.Serialization;

public partial class CalendarSerializer
{
    public static string Serialize<T>(
        T component,
        CalendarSerializerOptions? options = default) where T : CalendarComponent
    {
        using var memory = new MemoryStream();
        Serialize(memory, component, options);

        var buffer = memory.GetBuffer();

        // Max string length is int.MaxValue
        var length = (int) memory.Length;

        return Encoding.UTF8.GetString(buffer, 0, length);
    }

    public static void Serialize(
        Stream utf8Destination,
        CalendarComponent component,
        CalendarSerializerOptions? options = default)
    {
        var writer = new CalendarWriter(utf8Destination);

        options ??= new();

        writer.WriteRawContentLine("BEGIN"u8, component.Name);

        IEnumerable<ICalendarProperty> properties = component.Properties;
        if (options.OrderComponentProperties)
        {
            properties = properties.OrderBy(x => x.Name);
        }

        // Write properties
        foreach (var property in properties)
        {
            if (property.Value is not { } propertyValue)
            {
                continue;
            }

            // Validate component properties
            if (component is CalendarComponent calendarComponent
                && !calendarComponent.ShouldSerializeProperty(property))
            {
                continue;
            }

            var converter = options.GetConverter(property.Name);

            if (converter.IsListValue)
            {
                // Write comma-separated list of values
                WritePropertyLine(writer, converter, property, property.Values);
            }
            else if (property.ValueCount > 1)
            {
                // Write multiple values as separate properties
                foreach (var singleValue in property.Values)
                {
                    if (singleValue is not null)
                    {
                        WritePropertyLine(writer, converter, property, singleValue);
                    }
                }
            }
            else
            {
                // Write a single value property
                WritePropertyLine(writer, converter, property, propertyValue);
            }
        }

        // Write children
        foreach (var child in component.Children)
        {
            if (child is CalendarComponent childComponent)
            {
                Serialize(utf8Destination, childComponent, options);
            }
        }

        writer.WriteRawContentLine("END"u8, component.Name);
    }

    private static void WritePropertyLine(
        CalendarWriter writer,
        CalendarConverter converter,
        ICalendarProperty property,
        object propertyValue)
    {
        writer.WriteName(property.Name);

        try
        {
            // The parameter proxy system has issues. If the value is a proxy,
            // use the proxy as the parameters instead of the actual property
            // paramters.
            ICalendarParameterCollectionContainer parameterContainer = propertyValue is CalendarDataType calDataValue
                ? calDataValue : property;

            converter.WriteObjectParameters(writer, parameterContainer);
        }
        catch (Exception ex) when (ex is not SerializationException)
        {
            throw new SerializationException(
                $"Failed to write property parameters for \"{property.Name}\"", ex);
        }

        if (converter.IsValueBase64(property))
        {
            writer.WriteNextValueAsBase64();
        }

        try
        {
            converter.WriteObject(writer, propertyValue);
        }
        catch (InvalidCastException)
        {
            throw new SerializationException($"Value type of property \"{property.Name}\""
                + $" does not match converter type");
        }
        catch (Exception ex) when (ex is not SerializationException)
        {
            throw new SerializationException(
                $"Failed to write value for property \"{property.Name}\". See inner exception.", ex);
        }

        writer.EndLine();
    }
}

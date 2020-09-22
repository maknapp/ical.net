using Ical.Net.Collections.Interfaces;
using Ical.Net.DataTypes;

namespace Ical.Net
{
    public interface ICalendarProperty : IParameterContainer, ICalendarObject, ICalendarValue<object>
    {
        object Value { get; }
    }
}

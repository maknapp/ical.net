using Ical.Net.Collections.Interfaces;
using Ical.Net.DataTypes;

namespace Ical.Net
{
    public interface ICalendarProperty : IParameterContainer, ICalendarObject, IValueObject<object>
    {
        object Value { get; set; }
    }
}

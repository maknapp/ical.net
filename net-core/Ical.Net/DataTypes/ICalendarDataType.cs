using System;

namespace Ical.Net.DataTypes
{
    public interface ICalendarDataType : IParameterContainer, ICopyable
    {
        Type GetValueType();
        void SetValueType(string type);
        ICalendarObject AssociatedObject { get; set; }
        Calendar Calendar { get; }

        string Language { get; set; }
    }
}

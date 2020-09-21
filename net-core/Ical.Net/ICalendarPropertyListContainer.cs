namespace Ical.Net
{
    public interface ICalendarPropertyListContainer : ICalendarObject
    {
        ICalendarPropertyList Properties { get; }
    }
}

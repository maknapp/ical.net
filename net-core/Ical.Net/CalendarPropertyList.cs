using System.Linq;
using Ical.Net.Collections;

namespace Ical.Net
{
    public class CalendarPropertyList : GroupedValueList<string, ICalendarProperty, CalendarProperty, object>
    {
        private readonly ICalendarObject _parent;

        public CalendarPropertyList() {}

        public CalendarPropertyList(ICalendarObject parent)
        {
            _parent = parent;
            ItemAdded += CalendarPropertyList_ItemAdded;
        }

        private void CalendarPropertyList_ItemAdded(object sender, ItemAddedEventArgs<ICalendarProperty> e)
        {
            e.Item.Parent = _parent;
        }

        public ICalendarProperty this[string name] => ContainsKey(name)
            ? AllOf(name).FirstOrDefault()
            : null;
    }
}
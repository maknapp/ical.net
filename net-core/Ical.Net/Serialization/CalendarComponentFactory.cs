using System;
using Ical.Net.CalendarComponents;

namespace Ical.Net.Serialization
{
    public sealed class CalendarComponentFactory
    {
        public ICalendarComponent Build(string componentName)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                throw new ArgumentException($"{nameof(componentName)} cannot be null, empty or whitespace.", nameof(componentName));
            }

            ICalendarComponent component;
            string name = componentName.ToUpper();

            switch (name)
            {
                case Components.Alarm:
                    component = new Alarm();
                    break;
                case EventStatus.Name:
                    component = new CalendarEvent();
                    break;
                case Components.Freebusy:
                    component = new FreeBusy();
                    break;
                case JournalStatus.Name:
                    component = new Journal();
                    break;
                case Components.Timezone:
                    component = new VTimeZone();
                    break;
                case TodoStatus.Name:
                    component = new Todo();
                    break;
                case Components.Calendar:
                    component = new Calendar();
                    break;
                default:
                    component = new CalendarComponent();
                    break;
            }

            component.Name = name;
            return component;
        }
    }
}

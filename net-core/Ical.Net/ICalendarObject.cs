using Ical.Net.Collections;

namespace Ical.Net
{
    public interface ICalendarObject : IGroupedObject<string>, ITypedServiceProvider, INamedServiceProvider
    {
        /// <summary>
        /// The name of the calendar object.  Every calendar object can be assigned a name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Returns the parent of this object.
        /// </summary>
        ICalendarObject Parent { get; set; }

        /// <summary>
        /// Returns a collection of children of this object.
        /// </summary>
        ICalendarObjectList<ICalendarObject> Children { get; }

        /// <summary>
        /// Returns the iCalendar that this object is associated with.
        /// </summary>
        Calendar Calendar { get; }
    }
}

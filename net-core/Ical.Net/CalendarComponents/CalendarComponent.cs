using System.Diagnostics;
using System.Runtime.Serialization;

namespace Ical.Net.CalendarComponents
{
    /// <summary>
    /// This class is used by the parsing framework for iCalendar components.
    /// Generally, you should not need to use this class directly.
    /// </summary>
    [DebuggerDisplay("Component: {Name}")]
    public class CalendarComponent : CalendarObject, ICalendarComponent
    {
        /// <summary>
        /// Returns a list of properties that are associated with the iCalendar object.
        /// </summary>
        public CalendarPropertyList Properties { get; protected set; }

        public CalendarComponent() : base()
        {
            Initialize();
        }

        public CalendarComponent(string name) : base(name)
        {
            Initialize();
        }

        private void Initialize()
        {
            Properties = new CalendarPropertyList(this);
        }

        protected override void OnDeserializing(StreamingContext context)
        {
            base.OnDeserializing(context);

            Initialize();
        }

        public override void CopyFrom(ICopyable obj)
        {
            base.CopyFrom(obj);

            var calendarComponent = obj as ICalendarComponent;
            if (calendarComponent == null)
            {
                return;
            }

            Properties.Clear();
            foreach (var property in calendarComponent.Properties)
            {
                Properties.Add(property);
            }
        }

        /// <summary>
        /// Adds a property to this component.
        /// </summary>
        public void AddProperty(string name, string value)
        {
            AddProperty(new CalendarProperty(name, value));
        }

        /// <summary>
        /// Adds a property to this component.
        /// </summary>
        public void AddProperty(ICalendarProperty property)
        {
            property.Parent = this;
            Properties.Set(property.Name, property.Value);
        }
    }
}
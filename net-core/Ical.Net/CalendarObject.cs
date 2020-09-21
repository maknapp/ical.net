using System;
using System.Runtime.Serialization;
using Ical.Net.Collections;

namespace Ical.Net
{
    /// <summary>
    /// The base class for all iCalendar objects and components.
    /// </summary>
    public class CalendarObject : CalendarObjectBase, ICalendarObject
    {
        private TypedServiceProvider _typedServices;
        private NamedServiceProvider _namedServices;

        internal CalendarObject()
        {
            Initialize();
        }

        public CalendarObject(string name) : this()
        {
            Name = name;
        }

        public CalendarObject(int line, int col) : this()
        {
            Line = line;
            Column = col;
        }

        private void Initialize()
        {
            _typedServices = new TypedServiceProvider();
            _namedServices = new NamedServiceProvider();

            // TODO: I'm fairly certain this is ONLY used for null checking. If so, maybe it can just be a bool? CalendarObjectList is an empty object, and
            // TODO: its constructor parameter is ignored
            Children = new CalendarObjectList();
            Children.ItemAdded += Children_ItemAdded;
        }

        [OnDeserializing]
        internal void DeserializingInternal(StreamingContext context) => OnDeserializing(context);

        [OnDeserialized]
        internal void DeserializedInternal(StreamingContext context) => OnDeserialized(context);

        protected virtual void OnDeserializing(StreamingContext context) => Initialize();

        protected virtual void OnDeserialized(StreamingContext context) {}

        private void Children_ItemAdded(object sender, ItemAddedEventArgs<ICalendarObject> e) => e.Item.Parent = this;

        protected bool Equals(CalendarObject other) => string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((CalendarObject) obj);
        }

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;

        public override void CopyFrom(ICopyable copyable)
        {
            var calendarObject = copyable as ICalendarObject;
            if (calendarObject == null)
            {
                return;
            }

            // Copy the name and basic information
            Name = calendarObject.Name;
            Parent = calendarObject.Parent;
            Line = calendarObject.Line;
            Column = calendarObject.Column;

            // Add each child
            Children.Clear();
            foreach (var child in calendarObject.Children)
            {
                Children.Add(child);
            }
        }

        /// <summary>
        /// Returns the parent iCalObject that owns this one.
        /// </summary>
        public ICalendarObject Parent { get; set; }

        /// <summary>
        /// A collection of iCalObjects that are children of the current object.
        /// </summary>
        public ICalendarObjectList<ICalendarObject> Children { get; private set; }

        /// <summary>
        /// Gets or sets the name of the iCalObject.  For iCalendar components, this is the RFC 5545 name of the component.
        /// </summary>        
        public string Name { get; set; }

        /// <summary>
        /// Returns the <see cref="Calendar"/> that this DDayiCalObject belongs to.
        /// </summary>
        public Calendar Calendar
        {
            get
            {
                ICalendarObject obj = this;
                while (!(obj is Calendar) && obj.Parent != null)
                {
                    obj = obj.Parent;
                }

                return obj as Calendar;
            }
        }

        public string Group
        {
            get => Name;
            set => Name = value;
        }

        public int Line { get; set; }

        public int Column { get; set; }

        public object GetService(Type serviceType)
            => _typedServices.GetService(serviceType);

        public T GetService<T>()
            => _typedServices.GetService<T>();

        public void SetService(object obj)
            => _typedServices.SetService(obj);

        public void RemoveService(Type serviceType)
            => _typedServices.RemoveService(serviceType);

        public object GetService(string name)
            => _namedServices.GetService(name);

        public T GetService<T>(string name)
            => _namedServices.GetService<T>(name);

        public void SetService(string name, object obj)
            => _namedServices.SetService(name, obj);

        public void RemoveService(string name)
            => _namedServices.RemoveService(name);
    }
}

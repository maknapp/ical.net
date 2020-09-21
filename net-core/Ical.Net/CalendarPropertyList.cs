using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ical.Net.Collections;

namespace Ical.Net
{
    public interface ICalendarPropertyList: IEnumerable<ICalendarProperty>
    {
        T Get<T>(string group);
        IList<T> GetMany<T>(string group);
        IEnumerable<ICalendarProperty> AllOf(string group);
        void Clear();
        bool ContainsKey(string group);
        bool Contains(ICalendarProperty item);
        void Set(string group, object value);
        void Set(string group, IEnumerable<object> values);
        void Add(ICalendarProperty item);
        bool Remove(string group);
        ICalendarProperty this[string name] { get; }
    }

    public class CalendarPropertyList : ICalendarPropertyList
    {
        private readonly ICalendarObject _parent;
        private readonly GroupedValueList<string, ICalendarProperty, CalendarProperty, object> _list;

        public CalendarPropertyList(ICalendarObject parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _list = new GroupedValueList<string, ICalendarProperty, CalendarProperty, object>();
            _list.ItemAdded += CalendarPropertyList_ItemAdded;
        }

        private void CalendarPropertyList_ItemAdded(object sender, ItemAddedEventArgs<ICalendarProperty> e)
        {
            e.Item.Parent = _parent;
        }

        public T Get<T>(string group) 
            => _list.Get<T>(group);

        public IList<T> GetMany<T>(string group)
            => _list.GetMany<T>(group);

        public IEnumerable<ICalendarProperty> AllOf(string group)
            => _list.AllOf(group);

        public void Clear()
            => _list.Clear();

        public bool ContainsKey(string group)
            => _list.ContainsKey(group);

        public bool Contains(ICalendarProperty item)
            => _list.Contains(item);

        public void Set(string group, object value)
            => _list.Set(group, value);

        public void Set(string group, IEnumerable<object> values)
            => _list.Set(group, values);

        public void Add(ICalendarProperty item)
            => _list.Add(item);

        public bool Remove(string group)
            => _list.Remove(group);

        public ICalendarProperty this[string name] 
            => _list.ContainsKey(name) ? _list.AllOf(name).FirstOrDefault() : null;

        public IEnumerator<ICalendarProperty> GetEnumerator() 
            => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }
}

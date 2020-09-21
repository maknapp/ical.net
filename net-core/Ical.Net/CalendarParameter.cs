using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Ical.Net.Collections.Interfaces;

namespace Ical.Net
{
    [DebuggerDisplay("{Name}={string.Join(\",\", Values)}")]
    public sealed class CalendarParameter : CalendarObject, ICalendarValue<string>
    {
        private StringCalendarValue _value;

        public CalendarParameter()
        {
            Initialize();
        }

        public CalendarParameter(string name) : base(name)
        {
            Initialize();
        }

        public CalendarParameter(string name, string value) : base(name)
        {
            Initialize();
            AddValue(value);
        }

        public CalendarParameter(string name, IEnumerable<string> values) : base(name)
        {
            Initialize();
            foreach (var v in values)
            {
                AddValue(v);
            }
        }

        private void Initialize()
        {
            _value = new StringCalendarValue();
        }

        protected override void OnDeserializing(StreamingContext context)
        {
            base.OnDeserializing(context);
            Initialize();
        }

        public override void CopyFrom(ICopyable c)
        {
            base.CopyFrom(c);

            var p = c as CalendarParameter;
            if (p?.Values == null)
            {
                return;
            }

            _value = new StringCalendarValue(p.Values);
        }

        public IEnumerable<string> Values
            => _value.Values;

        public int ValueCount
            => _value.ValueCount;

        public bool ContainsValue(string value)
            => _value.ContainsValue(value);

        public void SetValue(string value)
            => _value.SetValue(value);

        public void SetValue(IEnumerable<string> values)
            => _value.SetValue(values);

        public void AddValue(string value)
            => _value.AddValue(value);

        public void RemoveValue(string value)
            => _value.RemoveValue(value);

        public string Value
        {
            get => _value?.Values.FirstOrDefault();
            set => SetValue(value);
        }

        private sealed class StringCalendarValue : ICalendarValue<string>
        {
            private readonly HashSet<string> _values;

            public StringCalendarValue(IEnumerable<string> values)
            {
                if (values == null)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                _values = new HashSet<string>(values.Where(IsValidValue), StringComparer.OrdinalIgnoreCase);
            }

            public StringCalendarValue()
            {
                _values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            public IEnumerable<string> Values
                => _values.ToArray();

            public int ValueCount
                => _values?.Count ?? 0;

            public bool ContainsValue(string value)
                => _values.Contains(value);

            public void SetValue(string value)
            {
                _values.Add(value);
            }

            public void SetValue(IEnumerable<string> values)
            {
                // Remove all previous values
                _values.Clear();
                _values.UnionWith(values.Where(IsValidValue));
            }

            public void AddValue(string value)
            {
                if (!IsValidValue(value))
                {
                    return;
                }
                _values.Add(value);
            }

            public void RemoveValue(string value)
            {
                _values.Remove(value);
            }

            public static bool IsValidValue(string value)
                => !string.IsNullOrWhiteSpace(value);
        }
    }
}

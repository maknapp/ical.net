using System;
using Ical.Net.Serialization.DataTypes;

namespace Ical.Net.DataTypes
{
    /// <summary> Represents an iCalendar period of time. </summary>    
    public class Period : CalendarDataType, IComparable<Period>
    {
        public Period() { }

        public Period(IDateTime start, IDateTime end)
        {
            StartTime = start ?? throw new ArgumentNullException(nameof(start));
            if (end != null && end.LessThanOrEqual(start))
            {
                throw new ArgumentException($"Start time ({start}) must be before the end time ({end})");
            }

            if (end == null)
            {
                return;
            }
            EndTime = end;
            Duration = end.Subtract(start);
        }

        public Period(IDateTime start, TimeSpan duration = default)
        {
            if (duration < TimeSpan.Zero)
            {
                throw new ArgumentException($"'{nameof(duration)}' ({duration}) cannot be negative");
            }

            StartTime = start ?? throw new ArgumentNullException(nameof(start));
            if (duration == default)
            {
                return;
            }

            Duration = duration;
            EndTime = start.Add(duration);
        }

        public override void CopyFrom(ICopyable obj)
        {
            base.CopyFrom(obj);

            var p = obj as Period;
            if (p == null)
            {
                return;
            }
            StartTime = p.StartTime;
            EndTime = p.EndTime;
            Duration = p.Duration;
        }

        protected bool Equals(Period other) => Equals(StartTime, other.StartTime) && Equals(EndTime, other.EndTime) && Duration.Equals(other.Duration);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Period) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StartTime?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (EndTime?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ Duration.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            var periodSerializer = new PeriodSerializer();
            return periodSerializer.Serialize(this);
        }

        private void ExtrapolateTimes()
        {
            if (EndTime == null && StartTime != null && Duration != default)
            {
                EndTime = StartTime.Add(Duration);
            }
            else if (Duration == default && StartTime != null && EndTime != null)
            {
                Duration = EndTime.Subtract(StartTime);
            }
            else if (StartTime == null && Duration != default && EndTime != null)
            {
                StartTime = EndTime.Subtract(Duration);
            }
        }

        private IDateTime _startTime;
        public IDateTime StartTime
        {
            get => _startTime.HasTime
                ? _startTime
                : new CalDateTime(new DateTime(_startTime.Value.Year, _startTime.Value.Month, _startTime.Value.Day, 0, 0, 0), _startTime.TzId);
            set
            {
                if (Equals(_startTime, value))
                {
                    return;
                }
                _startTime = value;
                ExtrapolateTimes();
            }
        }

        private IDateTime _endTime;
        public IDateTime EndTime
        {
            get => _endTime;
            set
            {
                if (Equals(_endTime, value))
                {
                    return;
                }
                _endTime = value;
                ExtrapolateTimes();
            }
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get
            {
                if (StartTime != null
                    && EndTime == null
                    && StartTime.Value.TimeOfDay == TimeSpan.Zero)
                {
                    return TimeSpan.FromDays(1);
                }
                return _duration;
            }
            set
            {
                if (Equals(_duration, value))
                {
                    return;
                }
                _duration = value;
                ExtrapolateTimes();
            }
        }

        public bool Contains(IDateTime dt)
        {
            // Start time is inclusive
            if (dt == null || StartTime == null || !StartTime.LessThanOrEqual(dt))
            {
                return false;
            }

            // End time is exclusive
            return EndTime == null || EndTime.GreaterThan(dt);
        }

        public bool CollidesWith(Period period) => period != null
            && ((period.StartTime != null && Contains(period.StartTime)) || (period.EndTime != null && Contains(period.EndTime)));

        public int CompareTo(Period other)
        {
            if (other == null)
            {
                return 1;
            }

            if (StartTime == null)
            {
                return -1;
            }

            if (StartTime.Equals(other.StartTime))
            {
                return 0;
            }

            if (StartTime.LessThan(other.StartTime))
            {
                return -1;
            }

            return 1;
        }
    }
}

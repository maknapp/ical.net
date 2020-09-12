using System;
using System.IO;
using Ical.Net.Serialization.DataTypes;

namespace Ical.Net.DataTypes
{
    /// <summary>
    /// A class that is used to specify exactly when an <see cref="Components.Alarm"/> component will trigger.
    /// Usually this date/time is relative to the component to which the Alarm is associated.
    /// </summary>    
    public sealed class Trigger : EncodableDataType
    {
        private IDateTime _dateTime;
        private TimeSpan? _duration;

        public IDateTime DateTime
        {
            get => _dateTime;
            set
            {
                _dateTime = value;
                if (_dateTime == null)
                {
                    return;
                }

                // NOTE: this, along with the "Duration" setter, fixes the bug tested in
                // TODO11(), as well as this thread: https://sourceforge.net/forum/forum.php?thread_id=1926742&forum_id=656447

                // DateTime and Duration are mutually exclusive
                Duration = null;

                // Do not allow timeless date/time values
                _dateTime.HasTime = true;
            }
        }

        public TimeSpan? Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                if (_duration != null)
                {
                    // NOTE: see above.

                    // DateTime and Duration are mutually exclusive
                    DateTime = null;
                }
            }
        }

        public string Related { get; set; } = TriggerRelation.Start;

        public bool IsRelative => _duration != null;

        public Trigger() {}

        public Trigger(TimeSpan ts)
        {
            Duration = ts;
        }

        public Trigger(string value) : this()
        {
            var serializer = new TriggerSerializer();
            CopyFrom(serializer.Deserialize(new StringReader(value)) as ICopyable);
        }

        public override void CopyFrom(ICopyable obj)
        {
            base.CopyFrom(obj);
            if (!(obj is Trigger))
            {
                return;
            }

            var t = (Trigger) obj;
            DateTime = t.DateTime;
            Duration = t.Duration;
            Related = t.Related;
        }

        public bool Equals(Trigger other) => Equals(_dateTime, other._dateTime) && _duration.Equals(other._duration) && Related == other.Related;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((Trigger) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _dateTime?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ _duration.GetHashCode();
                hashCode = (hashCode * 397) ^ Related?.GetHashCode() ?? 0;
                return hashCode;
            }
        }
    }
}
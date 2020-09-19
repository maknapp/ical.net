using System;

namespace Ical.Net.DataTypes
{
    /// <summary>
    /// Represents a time offset from UTC (Coordinated Universal Time).
    /// </summary>
    public sealed class UtcOffset : EncodableDataType
    {
        public TimeSpan Offset { get; }

        public bool Positive => Offset >= TimeSpan.Zero;

        public int Hours => Math.Abs(Offset.Hours);

        public int Minutes => Math.Abs(Offset.Minutes);

        public int Seconds => Math.Abs(Offset.Seconds);
        
        public UtcOffset(TimeSpan offset)
        {
            Offset = offset;
        }

        public static implicit operator UtcOffset(TimeSpan value) => new UtcOffset(value);

        public static explicit operator TimeSpan(UtcOffset value) => value.Offset;
        
        protected bool Equals(UtcOffset other) => Offset == other.Offset;

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
            return Equals((UtcOffset) obj);
        }

        public override int GetHashCode() => Offset.GetHashCode();

        public override string ToString() => (Positive ? "+" : "-") + Hours.ToString("00") + Minutes.ToString("00") + (Seconds != 0 ? Seconds.ToString("00") : string.Empty);
    }
}

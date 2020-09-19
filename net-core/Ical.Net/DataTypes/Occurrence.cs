using System;
using Ical.Net.CalendarComponents;

namespace Ical.Net.DataTypes
{
    public sealed class Occurrence
    {
        public Period Period { get; }

        public IRecurrable Source { get; }
        
        public Occurrence(IRecurrable recurrable, Period period)
        {
            Source = recurrable ?? throw new ArgumentNullException(nameof(recurrable));
            Period = period ?? throw new ArgumentNullException(nameof(period));
        }

        public bool Equals(Occurrence other) => Equals(Period, other.Period) && Equals(Source, other.Source);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is Occurrence && Equals((Occurrence) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Period?.GetHashCode() ?? 0) * 397) ^ (Source?.GetHashCode() ?? 0);
            }
        }
    }
}

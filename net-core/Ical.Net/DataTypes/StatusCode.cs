using System;
using System.Linq;
using Ical.Net.Utilities;

namespace Ical.Net.DataTypes
{
    /// <summary>
    /// An iCalendar status code.
    /// </summary>
    public sealed class StatusCode : CalendarDataType
    {
        public int[] Parts { get; private set; }

        public int Primary => Parts.Length > 0 ? Parts[0] : 0;

        public int Secondary => Parts.Length > 1 ? Parts[1] : 0;

        public int Tertiary => Parts.Length > 2 ? Parts[2] : 0;

        public StatusCode() {}

        public StatusCode(int[] parts)
        {
            Parts = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        public override void CopyFrom(ICopyable obj)
        {
            base.CopyFrom(obj);

            if (obj is StatusCode sc)
            {
                Parts = new int[sc.Parts.Length];
                sc.Parts.CopyTo(Parts, 0);
            }
        }

        public bool Equals(StatusCode other) => Parts.SequenceEqual(other.Parts);

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
            return Equals((StatusCode) obj);
        }

        public override int GetHashCode() => CollectionHelpers.GetHashCode(Parts);
    }
}

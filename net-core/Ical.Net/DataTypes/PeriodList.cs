using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ical.Net.Evaluation;
using Ical.Net.Serialization.DataTypes;
using Ical.Net.Utilities;

namespace Ical.Net.DataTypes
{
    /// <summary>
    /// An iCalendar list of recurring dates (or date exclusions)
    /// </summary>
    public sealed class PeriodList : EncodableDataType, IReadOnlyList<Period>
    {
        private readonly IList<Period> _periods;

        public PeriodList()
        {
            _periods = new List<Period>();
            SetService(new PeriodListEvaluator(this));
        }

        public PeriodList(string timezoneId) : this()
        {
            TzId = timezoneId;
        }

        public string TzId { get; set; }
        public int Count => _periods.Count;

        public override void CopyFrom(ICopyable obj)
        {
            base.CopyFrom(obj);
            if (!(obj is PeriodList list))
            {
                return;
            }

            foreach (var period in list)
            {
                Add(period);
            }
        }

        public override string ToString() => new PeriodListSerializer().SerializeToString(this);

        public void Add(IDateTime dt) => _periods.Add(new Period(dt));

        public static IDictionary<string, List<Period>> GetGroupedPeriods(IEnumerable<PeriodList> periodLists)
        {
            // In order to know if two events are equal, a semantic understanding of exdates, rdates, rrules, and exrules is required. This could be done by
            // computing the complete recurrence set (expensive) while being time-zone sensitive, or by comparing each List<Period> in each IPeriodList.

            // For example, events containing these rules generate the same recurrence set, including having the same time zone for each occurrence, so
            // they're the same:
            // Event A:
            // RDATE:20170302T060000Z,20170303T060000Z
            // Event B:
            // RDATE:20170302T060000Z
            // RDATE:20170303T060000Z

            var grouped = new Dictionary<string, HashSet<Period>>(StringComparer.OrdinalIgnoreCase);
            foreach (var periodList in periodLists)
            {
                var defaultBucket = string.IsNullOrWhiteSpace(periodList.TzId) ? "" : periodList.TzId;

                foreach (var period in periodList)
                {
                    var actualBucket = string.IsNullOrWhiteSpace(period.StartTime.TzId) ? defaultBucket : period.StartTime.TzId;

                    if (!grouped.ContainsKey(actualBucket))
                    {
                        grouped.Add(actualBucket, new HashSet<Period>());
                    }
                    grouped[actualBucket].Add(period);
                }
            }
            return grouped.ToDictionary(
                k => k.Key, 
                v => v.Value.OrderBy(d => d.StartTime).ToList());
        }

        public bool Equals(PeriodList other)
        {
            return string.Equals(TzId, other.TzId, StringComparison.OrdinalIgnoreCase)
                   && CollectionHelpers.Equals(_periods, other._periods);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((PeriodList)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = TzId?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ CollectionHelpers.GetHashCode(_periods);
                return hashCode;
            }
        }

        public Period this[int index] => _periods[index];

        public void Clear() => _periods.Clear();
        public void Add(Period item) => _periods.Add(item);
        public bool Remove(Period item) => _periods.Remove(item);

        public IEnumerator<Period> GetEnumerator() => _periods.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _periods.GetEnumerator();
    }
}

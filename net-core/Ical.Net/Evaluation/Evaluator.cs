using System;
using System.Collections.Generic;
using System.Globalization;
using Ical.Net.DataTypes;
using Ical.Net.Utilities;

namespace Ical.Net.Evaluation
{
    public abstract class Evaluator : IEvaluator
    {
        private ICalendarObject _associatedObject;
        private readonly ICalendarDataType _associatedDataType = null;

        protected Evaluator()
        {
            Calendar = CultureInfo.CurrentCulture.Calendar;
            Periods = new HashSet<Period>();
        }
        
        protected IDateTime ConvertToIDateTime(DateTime dt, IDateTime referenceDate)
        {
            IDateTime newDt = new CalDateTime(dt, referenceDate.TzId);
            newDt.AssociateWith(referenceDate);
            return newDt;
        }

        protected void IncrementDate(ref DateTime dt, RecurrencePattern pattern, int interval)
        {
            if (interval == 0)
            {
                // TODO: Throw a more specific exception.
                throw new Exception("Cannot evaluate with an interval of zero.  Please use an interval other than zero.");
            }

            var old = dt;
            switch (pattern.Frequency)
            {
                case FrequencyType.Secondly:
                    dt = old.AddSeconds(interval);
                    break;
                case FrequencyType.Minutely:
                    dt = old.AddMinutes(interval);
                    break;
                case FrequencyType.Hourly:
                    dt = old.AddHours(interval);
                    break;
                case FrequencyType.Daily:
                    dt = old.AddDays(interval);
                    break;
                case FrequencyType.Weekly:
                    dt = DateUtil.AddWeeks(old, interval, pattern.FirstDayOfWeek);
                    break;
                case FrequencyType.Monthly:
                    dt = old.AddDays(-old.Day + 1).AddMonths(interval);
                    break;
                case FrequencyType.Yearly:
                    dt = old.AddDays(-old.DayOfYear + 1).AddYears(interval);
                    break;

                // TODO: use a more specific exception.
                default:
                    throw new Exception("FrequencyType.NONE cannot be evaluated. Please specify a FrequencyType before evaluating the recurrence.");
            }
        }

        public System.Globalization.Calendar Calendar { get; }

        public DateTime EvaluationStartBounds { get; protected set; } = DateTime.MaxValue;

        public DateTime EvaluationEndBounds { get; protected set; } = DateTime.MinValue;

        public ICalendarObject AssociatedObject
        {
            get => _associatedObject ?? _associatedDataType?.AssociatedObject;
            protected set => _associatedObject = value;
        }

        public HashSet<Period> Periods { get; }

        public virtual void Clear()
        {
            EvaluationStartBounds = DateTime.MaxValue;
            EvaluationEndBounds = DateTime.MinValue;
            Periods.Clear();
        }

        public abstract HashSet<Period> Evaluate(IDateTime referenceDate, DateTime periodStart, DateTime periodEnd, bool includeReferenceDateInResults);
    }
}

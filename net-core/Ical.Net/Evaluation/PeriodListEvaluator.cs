using System;
using System.Collections.Generic;
using Ical.Net.DataTypes;

namespace Ical.Net.Evaluation
{
    public sealed class PeriodListEvaluator : Evaluator
    {
        private readonly PeriodList _periodList;

        public PeriodListEvaluator(PeriodList rdt)
        {
            _periodList = rdt;
        }

        public override HashSet<Period> Evaluate(IDateTime referenceDate, DateTime periodStart, DateTime periodEnd, bool includeReferenceDateInResults)
        {
            var periods = new HashSet<Period>();

            if (includeReferenceDateInResults)
            {
                periods.Add(new Period(referenceDate));
            }

            if (periodEnd < periodStart)
            {
                return periods;
            }

            periods.UnionWith(_periodList);
            return periods;
        }
    }
}
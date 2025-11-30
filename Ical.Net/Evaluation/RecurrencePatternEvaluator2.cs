//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Ical.Net.DataTypes;
using Ical.Net.Utility;
using NodaTime;
using NodaTime.Calendars;
using NodaTime.Extensions;

namespace Ical.Net.Evaluation;
internal class RecurrencePatternEvaluator2
{
    private readonly RecurrencePattern rule;
    private readonly CalDateTime referenceDate;
    private readonly DateTimeZone timeZone;
    private readonly Instant? periodStart;
    private readonly EvaluationOptions? options;

    private readonly IsoDayOfWeek firstDayOfWeek;
    private readonly IWeekYearRule weekYearRule;
    private readonly ZonedDateTime zonedReferenceDate;
    private readonly int referenceWeekNo;

    public RecurrencePatternEvaluator2(
        RecurrencePattern rule,
        CalDateTime referenceDate,
        DateTimeZone timeZone,
        Instant? periodStart,
        EvaluationOptions? options)
    {
        this.rule = rule;
        this.referenceDate = referenceDate;
        this.timeZone = timeZone;
        this.periodStart = periodStart;
        this.options = options;

        firstDayOfWeek = rule.FirstDayOfWeek.ToIsoDayOfWeek();
        weekYearRule = WeekYearRules.ForMinDaysInFirstWeek(4, firstDayOfWeek);
        zonedReferenceDate = referenceDate.AsZonedOrDefault(timeZone);
        referenceWeekNo = weekYearRule.GetWeekOfWeekYear(zonedReferenceDate.Date);
    }

    public RecurrencePatternEvaluator2(
        RecurrencePattern rule,
        CalDateTime referenceDate,
        ZonedDateTime periodStart,
        EvaluationOptions? options)
        : this(rule, referenceDate, periodStart.Zone, periodStart.ToInstant(), options) { }

    public IEnumerable<EvaluationPeriod> Evaluate()
    {
        var count = 0L;

        // Determine where to start evaluation
        var seed = zonedReferenceDate;
        if (periodStart != null)
        {
            seed = SkipToByInterval(seed, periodStart.Value.InZone(seed.Zone));
        }

        // Instant that all results must be >= to
        var startInstant = zonedReferenceDate.ToInstant();

        var until = rule.Until?.ToZonedDateTime(timeZone).ToInstant();
        foreach (var value in BySetPosition(seed))
        {
            if (value.ToInstant() > until)
            {
                break;
            }

            if (value.ToInstant() < startInstant)
            {
                continue;
            }

            yield return new(value);

            if (++count >= rule.Count)
            {
                yield break;
            }
        }
    }


    /// <summary>
    /// Return value closest to 
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    /// <exception cref="EvaluationException"></exception>
    private ZonedDateTime SkipToByInterval(ZonedDateTime seed, ZonedDateTime limit)
    {
        var diff = limit - seed;

        if (diff <= NodaTime.Duration.Zero)
        {
            return seed;
        }

        // Handle time
        NodaTime.Duration time;
        switch (rule.Frequency)
        {
            case FrequencyType.Secondly:
                time = NodaTime.Duration.FromSeconds(FromInterval((long) diff.TotalSeconds, rule.Interval));
                return seed.Plus(time);
            case FrequencyType.Minutely:
                time = NodaTime.Duration.FromMinutes(FromInterval((long) diff.TotalMinutes, rule.Interval));
                return seed.Plus(time);
            case FrequencyType.Hourly:
                time = NodaTime.Duration.FromHours(FromInterval((long) diff.TotalHours, rule.Interval));
                return seed.Plus(time);
        }

        // Handle nominal
        NodaTime.Period nominalDiff;
        int nominalByInterval;
        switch (rule.Frequency)
        {
            case FrequencyType.Daily:
                nominalDiff = NodaTime.Period.Between(seed.Date, limit.Date, PeriodUnits.Days);
                nominalByInterval = nominalDiff.Days - (nominalDiff.Days % rule.Interval);
                return seed.LocalDateTime.PlusDays(nominalByInterval)
                    .InZoneLeniently(seed.Zone);
            case FrequencyType.Weekly:
                nominalDiff = NodaTime.Period.Between(seed.Date, limit.Date, PeriodUnits.Weeks);
                nominalByInterval = nominalDiff.Weeks - (nominalDiff.Weeks % rule.Interval);
                return seed.LocalDateTime.PlusWeeks(nominalByInterval)
                    .InZoneLeniently(seed.Zone);
            case FrequencyType.Monthly:
                nominalDiff = NodaTime.Period.Between(seed.Date, limit.Date, PeriodUnits.Months);
                var months = nominalDiff.Months - (nominalDiff.Months % rule.Interval);
                return seed.Date.PlusMonths(months)
                    .AtNearestDayOfMonth(zonedReferenceDate.Day)
                    .At(seed.TimeOfDay)
                    .InZoneLeniently(seed.Zone);
            case FrequencyType.Yearly:
                nominalDiff = NodaTime.Period.Between(seed.Date, limit.Date, PeriodUnits.Years);
                nominalByInterval = nominalDiff.Years - (nominalDiff.Years % rule.Interval);
                return seed.Date.PlusYears(nominalByInterval)
                    .AtNearestDayOfMonth(zonedReferenceDate.Day)
                    .At(seed.TimeOfDay)
                    .InZoneLeniently(seed.Zone);
        }

        throw new EvaluationException("Invalid frequency type");

        static long FromInterval(long value, int interval)
        {
            return value - (value % interval);
        }
    }

    public IEnumerable<ZonedDateTime> Expand(ZonedDateTime seed)
    {
        switch (rule.Frequency)
        {
            case FrequencyType.Secondly:
                while (true)
                {
                    yield return seed;
                    seed = seed.PlusSeconds(rule.Interval);
                }
            case FrequencyType.Minutely:
                while (true)
                {
                    yield return seed;
                    seed = seed.PlusMinutes(rule.Interval);
                }
            case FrequencyType.Hourly:
                while (true)
                {
                    yield return seed;
                    seed = seed.PlusHours(rule.Interval);
                }
            case FrequencyType.Daily:
                while (true)
                {
                    yield return seed;
                    seed = seed.LocalDateTime.PlusDays(rule.Interval)
                        .InZoneLeniently(seed.Zone);
                }
            case FrequencyType.Weekly:
                while (true)
                {
                    yield return seed;
                    seed = seed.LocalDateTime.PlusWeeks(rule.Interval)
                        .InZoneLeniently(seed.Zone);
                }
            case FrequencyType.Monthly:
                while (true)
                {
                    yield return seed;
                    seed = seed.Date.PlusMonths(rule.Interval)
                        .AtNearestDayOfMonth(zonedReferenceDate.Day)
                        .At(seed.TimeOfDay)
                        .InZoneLeniently(seed.Zone);
                }
            case FrequencyType.Yearly:
                while (true)
                {
                    yield return seed;
                    seed = seed.Date.PlusYears(rule.Interval)
                        .AtNearestDayOfMonth(zonedReferenceDate.Day)
                        .At(seed.TimeOfDay)
                        .InZoneLeniently(seed.Zone);
                }
        }
    }

    public IEnumerable<ZonedDateTime> BySetPosition(ZonedDateTime seed)
    {
        if (rule.BySetPosition.Count == 0)
        {
            return StartByRules(Expand(seed));
        }
        else
        {
            return LimitSetPosition(seed);
        }
    }

    public IEnumerable<ZonedDateTime> LimitSetPosition(ZonedDateTime seed)
    {
        var hasNegativeOffset = rule.BySetPosition.Any(static p => p < 0);

        if (hasNegativeOffset)
        {
            foreach (var setSeed in Expand(seed))
            {
                var recurrenceSet = StartByRules([setSeed]);

                // Evaluate the entire set so that negative offsets can be handled
                var setValues = recurrenceSet.ToList();

                // Normalize BYSETPOS values based on the evaluated set of values.
                // Note that this produces a list of zero-based INDEX values, not
                // the actual BYSETPOS values.
                var count = setValues.Count;
                var orderedSetPos = rule.BySetPosition
                    .Select(p => (p < 0) ? count + p : p - 1)
                    .ToArray();

                Array.Sort(orderedSetPos);

                // Yield the values from each set position
                for (var i = 0; i < orderedSetPos.Length && orderedSetPos[i] < setValues.Count; i++)
                {
                    yield return setValues[orderedSetPos[i]];
                }
            }
        }
        else
        {
            // The normalized BYSETPOS values are all positive and
            // will not change, so prepare them once before expanding.
            var orderedSetPos = rule.BySetPosition.ToArray();
            Array.Sort(orderedSetPos);

            foreach (var setSeed in Expand(seed))
            {
                var recurrenceSet = StartByRules([setSeed]);

                foreach (var value in FilterByIndex(recurrenceSet, orderedSetPos))
                {
                    yield return value;
                }
            }
        }

        static IEnumerable<ZonedDateTime> FilterByIndex(IEnumerable<ZonedDateTime> values, int[] bySetPosOrdered)
        {
            var x = 0;
            int setPos;
            var valueEnumerator = values.GetEnumerator();

            for (var i = 0; i < bySetPosOrdered.Length; i++)
            {
                setPos = bySetPosOrdered[i];

                // Move to the next position in the set
                do
                {
                    if (!valueEnumerator.MoveNext())
                    {
                        yield break;
                    }
                } while (++x < setPos);

                // Set position reached
                yield return valueEnumerator.Current;
            }
        }
    }

    public IEnumerable<ZonedDateTime> StartByRules(IEnumerable<ZonedDateTime> seed)
    {
        if (referenceDate.HasTime)
        {
            return BySecond(seed);
        }
        else
        {
            return ByDay(seed);
        }
    }

    public IEnumerable<ZonedDateTime> BySecond(IEnumerable<ZonedDateTime> seed)
    {
        if (rule.BySecond.Count == 0)
        {
            return ByMinute(seed);
        }
        else if (rule.Frequency > FrequencyType.Secondly)
        {
            return ExpandSecond(seed);
        }
        else
        {
            // BySecond always limits
            return LimitSecond(seed);
        }
    }

    public IEnumerable<ZonedDateTime> LimitSecond(IEnumerable<ZonedDateTime> seed)
    {
        foreach (var value in ByMinute(seed))
        {
            if (rule.BySecond.Contains(value.Second))
            {
                yield return value;
            }
        }
    }

    public IEnumerable<ZonedDateTime> ExpandSecond(IEnumerable<ZonedDateTime> seed)
    {
        var bySecond = rule.BySecond.ToArray();
        Array.Sort(bySecond);

        foreach (var value in ByMinute(seed))
        {
            foreach (var second in bySecond)
            {
                yield return value.Date
                    .At(new LocalTime(value.Hour, value.Minute, second))
                    .InZoneRelativeTo(value);
            }
        }
    }

    public IEnumerable<ZonedDateTime> ByMinute(IEnumerable<ZonedDateTime> seed)
    {
        if (rule.ByMinute.Count == 0)
        {
            return ByHour(seed);
        }
        else if (rule.Frequency > FrequencyType.Minutely)
        {
            return ExpandMinute(seed);
        }
        else
        {
            return LimitMinute(seed);
        }
    }

    public IEnumerable<ZonedDateTime> LimitMinute(IEnumerable<ZonedDateTime> seed)
    {
        foreach (var value in ByHour(seed))
        {
            if (rule.ByMinute.Contains(value.Minute))
            {
                yield return value;
            }
        }
    }

    public IEnumerable<ZonedDateTime> ExpandMinute(IEnumerable<ZonedDateTime> seed)
    {
        var byMinute = rule.ByMinute.ToArray();
        Array.Sort(byMinute);

        foreach (var value in ByHour(seed))
        {
            foreach (var minute in byMinute)
            {
                yield return value.Date
                    .At(new LocalTime(value.Hour, minute, value.Second))
                    .InZoneRelativeTo(value);
            }
        }
    }

    public IEnumerable<ZonedDateTime> ByHour(IEnumerable<ZonedDateTime> seed)
    {
        if (rule.ByHour.Count == 0)
        {
            return ByDay(seed);
        }
        else if (rule.Frequency > FrequencyType.Hourly)
        {
            return ExpandHour(seed);
        }
        else
        {
            return LimitHour(seed);
        }
    }


    public IEnumerable<ZonedDateTime> LimitHour(IEnumerable<ZonedDateTime> seed)
    {
        foreach (var value in ByDay(seed))
        {
            if (rule.ByHour.Contains(value.Hour))
            {
                yield return value;
            }
        }
    }

    public IEnumerable<ZonedDateTime> ExpandHour(IEnumerable<ZonedDateTime> seed)
    {
        var byHour = rule.ByHour.ToArray();
        Array.Sort(byHour);

        foreach (var value in ByDay(seed))
        {
            foreach (var hour in byHour)
            {
                yield return value.Date
                    .At(new LocalTime(hour, value.Minute, value.Second))
                    .InZoneRelativeTo(value);
            }
        }
    }

    /// <summary>
    /// All values generated from here MUST represent a day (not weeks, months, or years).
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    public IEnumerable<ZonedDateTime> ByDay(IEnumerable<ZonedDateTime> seed)
    {
        if (rule.ByDay.Count == 0)
        {
            if ((rule.Frequency == FrequencyType.Monthly
                || rule.Frequency == FrequencyType.Yearly)
                && rule.ByMonthDay.Count == 0
                && rule.ByYearDay.Count == 0
                && rule.ByWeekNo.Count == 0)
            {
                // These values represent months,
                // so they must be set to the reference day.
                return LimitDayByReferenceDay(seed);
            }
            else
            {
                return ByMonthDay(seed);
            }
        }
        else if (rule.Frequency == FrequencyType.Weekly)
        {
            return ExpandDayFromWeek(seed);
        }
        else if (rule.Frequency == FrequencyType.Monthly)
        {
            if (rule.ByMonthDay.Count > 0)
            {
                return LimitDayOfWeek(seed);
            }
            else
            {
                return ExpandDayFromContext(seed, FrequencyType.Monthly);
            }
        }
        else if (rule.Frequency == FrequencyType.Yearly)
        {
            if (rule.ByYearDay.Count > 0 || rule.ByMonthDay.Count > 0)
            {
                return LimitDayOfWeek(seed);
            }
            else if (rule.ByWeekNo.Count > 0)
            {
                return ExpandDayFromContext(seed, FrequencyType.Weekly);
            }
            else if (rule.ByMonth.Count > 0)
            {
                return ExpandDayFromContext(seed, FrequencyType.Monthly);
            }
            else
            {
                return ExpandDayFromContext(seed, FrequencyType.Yearly);
            }
        }
        else
        {
            return LimitDayOfWeek(seed);
        }
    }

    private IEnumerable<ZonedDateTime> LimitDayByReferenceDay(IEnumerable<ZonedDateTime> seed)
    {
        var referenceDay = zonedReferenceDate.Day;
        foreach (var value in ByMonthDay(seed))
        {
            // Make sure day matches reference day
            if (value.Day != referenceDay
                && referenceDay > value.Calendar.GetDaysInMonth(value.Year, value.Month))
            {
                // Day does not exist, skip
                continue;
            }

            yield return value;
        }
    }

    /// <summary>
    /// Limit by day of week. Offsets are not supported.
    /// Only used for DAILY or smaller.
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    private IEnumerable<ZonedDateTime> LimitDayOfWeek(IEnumerable<ZonedDateTime> seed)
    {
        if (rule.ByDay.Any(x => x.Offset != null))
        {
            throw new EvaluateException($"BYDAY offsets are not supported in {rule.Frequency}");
        }

        var byDay = rule.ByDay.Select(x => x.DayOfWeek.ToIsoDayOfWeek());

        foreach (var value in ByMonthDay(seed))
        {
            if (byDay.Any(weekDay => weekDay == value.DayOfWeek))
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// Expand by day of week. Offsets are not supported.
    /// Only used for WEEKLY.
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    private IEnumerable<ZonedDateTime> ExpandDayFromWeek(IEnumerable<ZonedDateTime> seed)
    {
        var normalizedDays = NormalizeDays();

        // The value represents a week OR a week within the month
        foreach (var value in ByMonth(seed))
        {
            var weekYear = weekYearRule.GetWeekYear(value.Date);
            var week = weekYearRule.GetWeekOfWeekYear(value.Date);

            foreach (var day in normalizedDays)
            {
                var date = weekYearRule.GetLocalDate(weekYear, week, day);

                // Only produce date within the same month, if required
                if (rule.ByMonth.Count == 0 || date.Month == value.Month)
                {
                    yield return date.At(value.TimeOfDay).InZoneLeniently(value.Zone);
                }
            }
        }
    }

    public IEnumerable<ZonedDateTime> ExpandDay(IEnumerable<ZonedDateTime> seed)
    {
        var normalizedDays = NormalizeDays();

        foreach (var value in ByMonthDay(seed))
        {
            var weekYear = weekYearRule.GetWeekYear(value.Date);
            var week = weekYearRule.GetWeekOfWeekYear(value.Date);

            foreach (var day in normalizedDays)
            {
                var localDate = weekYearRule.GetLocalDate(weekYear, week, day);

                yield return localDate.At(value.TimeOfDay).InZoneLeniently(value.Zone);
            }
        }
    }

    private List<IsoDayOfWeek> NormalizeDays()
    {
        var normalizedDays = rule.ByDay.Select(x => x.DayOfWeek.ToIsoDayOfWeek())
            .OrderBy(day => day)
            .ToList();

        var startIndex = normalizedDays.FindIndex(x => x >= firstDayOfWeek);
        if (startIndex > 0)
        {
            normalizedDays = [.. normalizedDays.Skip(startIndex), .. normalizedDays.Take(startIndex)];
        }

        return normalizedDays;
    }

    private IEnumerable<ZonedDateTime> ExpandDayFromContext(
        IEnumerable<ZonedDateTime> seed,
        FrequencyType expandFrequency)
    {
        var daysWithoutOffset = rule.ByDay
            .Where(x => x.Offset == null)
            .Select(x => x.DayOfWeek.ToIsoDayOfWeek())
            .OrderBy(x => x)
            .ToList();

        var daysWithOffset = rule.ByDay.Where(x => x.Offset.HasValue).ToList();

        foreach (var value in ByWeekNo(seed))
        {
            LocalDate start, end;
            if (expandFrequency == FrequencyType.Weekly)
            {
                var weekYear = weekYearRule.GetWeekYear(value.Date);
                var weekNo = weekYearRule.GetWeekOfWeekYear(value.Date);
                start = weekYearRule.GetLocalDate(weekYear, weekNo, firstDayOfWeek);
                end = start.PlusWeeks(1);

                if (rule.ByMonth.Count > 0)
                {
                    if (start.Month != value.Month)
                    {
                        start = new LocalDate(value.Year, value.Month, 1);
                    }
                    else if (end.Month != value.Month && end.Day != 1)
                    {
                        // End is exclusive, so just make sure it is 1 day outside the month
                        end = new LocalDate(value.Year, end.Month, 1);
                    }
                }
            }
            else if (expandFrequency == FrequencyType.Monthly)
            {
                start = new LocalDate(value.Year, value.Month, 1);
                end = start.PlusMonths(1);
            }
            else if (expandFrequency == FrequencyType.Yearly)
            {
                start = new LocalDate(value.Year, 1, 1);
                end = start.PlusYears(1);
            }
            else
            {
                throw new EvaluateException("Invalid expand of BYDAY");
            }

            var results = ExpandDayOfWeekWithoutOffset(start, end, daysWithoutOffset);

            if (daysWithOffset.Count > 0)
            {
                var offsetResults = GetDayOfWeekWithOffset(start, end, daysWithOffset);

                if (daysWithoutOffset.Count == 0)
                {
                    results = offsetResults;
                }
                else
                {
                    // Days with offsets need to be merged together
                    results = offsetResults
                        .OrderedMerge(results)
                        .OrderedDistinct();
                }
            }

            foreach (var result in results)
            {
                yield return result.At(value.TimeOfDay)
                        .InZoneLeniently(value.Zone);
            }
        }
    }

    private static IEnumerable<LocalDate> ExpandDayOfWeekWithoutOffset(
        LocalDate start,
        LocalDate end,
        List<IsoDayOfWeek> weekDays)
    {
        if (weekDays.Count == 0)
        {
            yield break;
        }

        var value = start;

        // Get the first date that matches a week day
        var i = weekDays.IndexOf(value.DayOfWeek);
        if (i < 0)
        {
            value = value.Next(weekDays[0]);
            i = 0;
        }

        while (true)
        {
            if (value >= end)
            {
                yield break;
            }

            yield return value;

            i = (i + 1) % weekDays.Count;
            value = value.Next(weekDays[i]);
        }
    }

    private static IEnumerable<LocalDate> GetDayOfWeekWithOffset(
        LocalDate start,
        LocalDate end,
        List<WeekDay> weekDays)
    {
        foreach (var weekDay in weekDays)
        {
            var dayOfWeek = weekDay.DayOfWeek.ToIsoDayOfWeek();

            if (weekDay.Offset > 0)
            {
                if (start.DayOfWeek != dayOfWeek)
                {
                    start = start.Next(dayOfWeek);
                }

                start = start.PlusWeeks(weekDay.Offset.Value - 1);

                if (start < end)
                {
                    yield return start;
                }
            }
            else if (weekDay.Offset < 0)
            {
                // Context end is exclusive, so move directly
                // to previous day of week
                end = end
                    .Previous(dayOfWeek)
                    .PlusWeeks(weekDay.Offset.Value + 1);

                if (end >= start)
                {
                    yield return end;
                }
            }
            else
            {
                throw new EvaluationException("Encountered a day offset of 0 which is not allowed.");
            }
        }
    }

    private IEnumerable<ZonedDateTime> ByMonthDay(IEnumerable<ZonedDateTime> seed)
    {
        if (rule.ByMonthDay.Count == 0)
        {
            return ByYearDay(seed);
        }
        else if (rule.Frequency == FrequencyType.Weekly)
        {
            throw new EvaluationException("BYMONTHDAY is not supported in WEEKLY");
        }
        else if (rule.Frequency > FrequencyType.Weekly)
        {
            if (rule.ByYearDay.Count > 0)
            {
                // Values will be days, so just limit
                return LimitMonthDay(seed);
            }
            else if (rule.ByWeekNo.Count > 0)
            {
                return ExpandMonthDayFromWeek(seed);
            }
            else
            {
                return ExpandMonthDayFromMonth(seed);
            }
        }
        else
        {
            return LimitMonthDay(seed);
        }

    }

    private IEnumerable<ZonedDateTime> LimitMonthDay(IEnumerable<ZonedDateTime> seed)
    {
        foreach (var value in ByYearDay(seed))
        {
            var daysInMonth = CalendarSystem.Iso.GetDaysInMonth(value.Year, value.Month);

            // Note that this does not order or validate ByMonthDay values
            // like normalizing does. The incoming day value is valid and
            // because equality is all that matters, invalid ByMonthDay
            // values do not matter.
            foreach (var monthDay in rule.ByMonthDay)
            {
                var byMonthDay = (monthDay > 0) ? monthDay : (daysInMonth + monthDay + 1);
                if (value.Day == byMonthDay)
                {
                    yield return value;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Expand by day of month from months.
    /// Supports MONTHLY, YEARLY, and YEARLY+ByMonth.
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    private IEnumerable<ZonedDateTime> ExpandMonthDayFromMonth(IEnumerable<ZonedDateTime> seed)
    {
        foreach (var value in ByMonth(seed))
        {
            var normalizedDays = NormalizeMonthDay(rule.ByMonthDay, value.Year, value.Month);

            foreach (var day in normalizedDays)
            {
                var localDate = new LocalDate(value.Year, value.Month, day);

                yield return localDate.At(value.TimeOfDay).InZoneLeniently(value.Zone);
            }
        }
    }

    /// <summary>
    /// Expand by day of month from week.
    /// Supports YEARLY+BYWEEKNO and YEARLY+BYMONTH+BYWEEKNO.
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    private IEnumerable<ZonedDateTime> ExpandMonthDayFromWeek(IEnumerable<ZonedDateTime> seed)
    {
        var currentYear = -1;
        var currentMonth = -1;
        int[] normalizedDays = [];

        foreach (var value in ByWeekNo(seed))
        {
            if (currentYear != value.Year || currentMonth != value.Month)
            {
                currentYear = value.Year;
                currentMonth = value.Month;
                normalizedDays = NormalizeMonthDay(rule.ByMonthDay, value.Year, value.Month);
            }

            var weekYear = weekYearRule.GetWeekYear(value.Date);
            var weeksInWeekYear = weekYearRule.GetWeeksInWeekYear(weekYear);
            var weekNo = weekYearRule.GetWeekOfWeekYear(value.Date);

            var result = weekYearRule.GetLocalDate(weekYear, weekNo, firstDayOfWeek);
            var end = result.PlusWeeks(1);

            do
            {
                if (normalizedDays.Contains(result.Day)
                    && (rule.ByMonth.Count == 0 || rule.ByMonth.Contains(result.Month)))
                {
                    yield return result.At(value.TimeOfDay).InZoneLeniently(value.Zone);
                }
            } while ((result = result.PlusDays(1)) < end);
        }
    }

    private static int[] NormalizeMonthDay(List<int> byMonthDay, int year, int month)
    {
        var daysInMonth = CalendarSystem.Iso.GetDaysInMonth(year, month);

        return byMonthDay
            .Select(monthDay => (monthDay > 0) ? monthDay : (daysInMonth + monthDay + 1))
            .Where(day => day > 0 && day <= daysInMonth)
            .OrderBy(day => day)
            .ToArray();
    }

    private IEnumerable<ZonedDateTime> ByYearDay(IEnumerable<ZonedDateTime> seed)
    {
        if (rule.ByYearDay.Count == 0)
        {
            return ByWeekNo(seed);
        }
        else if (rule.Frequency == FrequencyType.Yearly)
        {
            return ExpandYearDay(seed);
        }
        else if (rule.Frequency < FrequencyType.Daily)
        {
            return LimitYearDay(seed);
        }
        else
        {
            throw new EvaluationException($"BYYEARDAY is not supported with {rule.Frequency}");
        }
    }

    private IEnumerable<ZonedDateTime> LimitYearDay(IEnumerable<ZonedDateTime> seed)
    {
        var currentYear = -1;
        var shouldNormalize = true;
        var normalizedYearDays = new int[rule.ByYearDay.Count];

        // If all values are positive, just copy and sort once
        if (rule.ByYearDay.All(static x => x > 0))
        {
            shouldNormalize = false;
            for (var i = 0; i < normalizedYearDays.Length; i++)
            {
                normalizedYearDays[i] = rule.ByYearDay[i];
            }
            Array.Sort(normalizedYearDays);
        }

        foreach (var value in ByWeekNo(seed))
        {
            if (shouldNormalize && currentYear != value.Year)
            {
                currentYear = value.Year;
                NormalizeYearDays(rule.ByYearDay, value.Year, normalizedYearDays);
            }

            if (normalizedYearDays.Contains(value.DayOfYear))
            {
                yield return value;
            }
        }
    }

    private IEnumerable<ZonedDateTime> ExpandYearDay(IEnumerable<ZonedDateTime> seed)
    {
        var currentYear = -1;
        var shouldNormalize = true;
        var normalizedYearDays = new int[rule.ByYearDay.Count];

        // If all values are positive, just copy and sort once
        if (rule.ByYearDay.All(static x => x > 0))
        {
            shouldNormalize = false;
            for (var i = 0; i < normalizedYearDays.Length; i++)
            {
                normalizedYearDays[i] = rule.ByYearDay[i];
            }
            Array.Sort(normalizedYearDays);
        }

        foreach (var value in ByWeekNo(seed))
        {
            var valueWeekNo = weekYearRule.GetWeekOfWeekYear(value.Date);

            if (shouldNormalize && currentYear != value.Year)
            {
                currentYear = value.Year;
                NormalizeYearDays(rule.ByYearDay, value.Year, normalizedYearDays);
            }

            foreach (var day in normalizedYearDays)
            {
                var result = new LocalDate(value.Year, 1, 1).PlusDays(day - 1);

                // Ignore values outside of the calendar year
                if (result.Year != value.Year)
                {
                    continue;
                }

                // Limit by month if specified
                if (rule.ByMonth.Count > 0 && result.Month != value.Month)
                {
                    continue;
                }

                // Limit by weekNo is specified
                if (rule.ByWeekNo.Count > 0)
                {
                    var weekNo = weekYearRule.GetWeekOfWeekYear(result);

                    if (valueWeekNo != weekNo)
                    {
                        continue;
                    }
                }

                yield return result.At(value.TimeOfDay).InZoneLeniently(value.Zone);
            }
        }
    }

    private static void NormalizeYearDays(List<int> yearDays, int year, int[] normalizedYearDays)
    {
        var daysInYear = CalendarSystem.Iso.GetDaysInYear(year);
        for (var i = 0; i < normalizedYearDays.Length; i++)
        {
            var yearDay = yearDays[i];
            normalizedYearDays[i] = (yearDay > 0) ? yearDay : (daysInYear + yearDay + 1);
        }
        Array.Sort(normalizedYearDays);
    }

    private IEnumerable<ZonedDateTime> ByWeekNo(IEnumerable<ZonedDateTime> seed)
    {
        if (rule.ByWeekNo.Count == 0)
        {
            return ByMonth(seed);
        }
        else if (rule.Frequency == FrequencyType.Yearly)
        {
            return ExpandWeekNo(seed);
        }
        else
        {
            throw new EvaluationException($"BYWEEKNO is not supported with {rule.Frequency}");
        }
    }

    /// <summary>
    /// Expands yearly week
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    private IEnumerable<ZonedDateTime> ExpandWeekNo(IEnumerable<ZonedDateTime> seed)
    {
        var weeksInWeekYear = 0;
        var currentWeekYear = -1;
        var shouldNormalize = true;
        var normalizedWeeks = new int[rule.ByWeekNo.Count];

        static void NormalizeWeekNo(List<int> byWeekNo, int weeksInWeekYear, int[] normalizedWeeks)
        {
            for (var i = 0; i < normalizedWeeks.Length; i++)
            {
                var weekNo = byWeekNo[i];
                normalizedWeeks[i] = (weekNo >= 0) ? weekNo : weeksInWeekYear + weekNo + 1;
            }
            Array.Sort(normalizedWeeks);
        }

        // If all values are non-negative, just copy and sort once
        if (rule.ByWeekNo.All(static x => x >= 0))
        {
            shouldNormalize = false;
            for (var i = 0; i < normalizedWeeks.Length; i++)
            {
                normalizedWeeks[i] = rule.ByWeekNo[i];
            }
            Array.Sort(normalizedWeeks);
        }

        foreach (var value in ByMonth(seed))
        {
            var weekYear = GetWeekYearBasedOnReferenceWeekYear(value.Date);

            if (currentWeekYear != weekYear)
            {
                currentWeekYear = weekYear;
                weeksInWeekYear = weekYearRule.GetWeeksInWeekYear(weekYear);

                if (shouldNormalize)
                {
                    NormalizeWeekNo(rule.ByWeekNo, weeksInWeekYear, normalizedWeeks);
                }
            }

            foreach (var weekNo in normalizedWeeks)
            {
                if (weekNo > weeksInWeekYear)
                {
                    continue;
                }

                var result = weekYearRule.GetLocalDate(weekYear, weekNo, zonedReferenceDate.DayOfWeek);

                // If BYMONTH, verify month is the same
                if (rule.ByMonth.Count > 0 && value.Month != result.Month)
                {
                    continue;
                }

                yield return result.At(value.TimeOfDay).InZoneLeniently(value.Zone);
            }
        }
    }

    private int GetWeekYearBasedOnReferenceWeekYear(LocalDate date)
    {
        // Make sure the month and day is as close as possible
        // to the reference month and day so that week numbers
        // can be compared accurately.
        if (date.Month != zonedReferenceDate.Month || date.Day != zonedReferenceDate.Day)
        {
            date = zonedReferenceDate.Date
                .PlusYears(date.Year - zonedReferenceDate.Year);
        }

        var weekYear = weekYearRule.GetWeekYear(date);
        var valueWeekNo = weekYearRule.GetWeekOfWeekYear(date);

        // Adjust week year to match reference week. Reference dates
        // in the first and last week of a year may result in different
        // week years than what YEARLY intends.
        var weekNoDiff = valueWeekNo - referenceWeekNo;
        if (weekNoDiff > 1)
        {
            weekYear += 1;
        }
        else if (weekNoDiff < -1)
        {
            weekYear -= 1;
        }

        return weekYear;
    }

    private IEnumerable<ZonedDateTime> ByMonth(IEnumerable<ZonedDateTime> seed)
    {
        if (rule.ByMonth.Count == 0)
        {
            return seed;
        }
        else if (rule.Frequency == FrequencyType.Yearly)
        {
            return ExpandMonth(seed);
        }
        else
        {
            return LimitMonth(seed);
        }
    }

    private IEnumerable<ZonedDateTime> LimitMonth(IEnumerable<ZonedDateTime> seed)
    {
        foreach (var value in seed)
        {
            if (rule.ByMonth.Contains(value.Month))
            {
                yield return value;
            }
        }
    }

    private IEnumerable<ZonedDateTime> ExpandMonth(IEnumerable<ZonedDateTime> seed)
    {
        var byMonth = rule.ByMonth.ToArray();
        Array.Sort(byMonth);

        foreach (var value in seed)
        {
            foreach (var month in byMonth)
            {
                var daysInMonth = value.Calendar.GetDaysInMonth(value.Year, month);
                var day = Math.Min(daysInMonth, value.Day);

                var result = new LocalDate(value.Year, month, day)
                    .At(value.TimeOfDay)
                    .InZoneLeniently(value.Zone);

                yield return result;
            }
        }
    }
}

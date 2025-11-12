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

        if (rule.Until is not null)
        {
            var until = rule.Until.ToZonedDateTime(timeZone).ToInstant();

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
        else
        {
            foreach (var value in BySetPosition(seed))
            {
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
                return seed.LocalDateTime.PlusMonths(months)
                    .InZoneLeniently(seed.Zone);
            case FrequencyType.Yearly:
                nominalDiff = NodaTime.Period.Between(seed.Date, limit.Date, PeriodUnits.Years);
                nominalByInterval = nominalDiff.Years - (nominalDiff.Years % rule.Interval);
                return seed.LocalDateTime.PlusYears(nominalByInterval)
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
                var next = seed.Date.ToYearMonth();
                while (true)
                {
                    // Increment monthly with day relative to original day of month
                    var value = next
                        .OnDayOfMonth(Math.Min(seed.Calendar.GetDaysInMonth(next.Year, next.Month), seed.Day))
                        .At(seed.TimeOfDay)
                        .InZoneLeniently(seed.Zone);

                    yield return value;

                    next = next.PlusMonths(rule.Interval);
                }
            case FrequencyType.Yearly:
                while (true)
                {
                    yield return seed;
                    seed = seed.LocalDateTime.PlusYears(rule.Interval)
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
        var hasNegativeOffset = rule.BySetPosition.Any(p => p < 0);

        foreach (var setSeed in Expand(seed))
        {
            var recurrenceSet = StartByRules([setSeed]);

            HashSet<int> bySetPos;

            if (hasNegativeOffset)
            {
                var tmp = recurrenceSet.ToList();
                var count = tmp.Count;
                recurrenceSet = tmp;
                bySetPos = [.. rule.BySetPosition.Select(p => (p < 0) ? count + p + 1 : p)];
            }
            else
            {
                bySetPos = [.. rule.BySetPosition];
            }

            var i = 0;
            foreach (var value in recurrenceSet)
            {
                if (bySetPos.Contains(++i))
                {
                    yield return value;
                }
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
        foreach (var value in ByMinute(seed))
        {
            foreach (var second in rule.BySecond)
            {
                yield return (value.Date + new LocalTime(value.Hour, value.Minute, second))
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
        foreach (var value in ByHour(seed))
        {
            foreach (var minute in rule.ByMinute)
            {
                yield return (value.Date + new LocalTime(value.Hour, minute, value.Second))
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
        foreach (var value in ByDay(seed))
        {
            foreach (var hour in rule.ByHour)
            {
                yield return (value.Date + new LocalTime(hour, value.Minute, value.Second))
                    .InZoneRelativeTo(value);
            }
        }
    }

    /// <summary>
    /// All values generated MUST represent a day (not weeks, months, or years).
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
                // Special MONTHLY expand
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
                // Special WEEKLY expand
                return ExpandDayFromContext(seed, FrequencyType.Weekly);
            }
            else if (rule.ByMonth.Count > 0)
            {
                // Special MONTHLY expand
                return ExpandDayFromContext(seed, FrequencyType.Monthly);
            }
            else
            {
                // Special YEARLY expand
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

        foreach (var value in ByMonthDay(seed))
        {
            if (rule.ByDay.Any(weekDay => weekDay.DayOfWeek.Equals(value.DayOfWeek.ToDayOfWeek())))
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
        var normalizedDays = GetNormalizeDays();

        if (rule.ByMonth.Count > 0)
        {
            foreach (var value in ByMonth(seed))
            {
                // Value represents week within the month
                var weekYear = weekYearRule.GetWeekYear(value.Date);
                var week = weekYearRule.GetWeekOfWeekYear(value.Date);

                foreach (var day in normalizedDays)
                {
                    var date = weekYearRule.GetLocalDate(weekYear, week, day);

                    // Only produce date within the same month
                    if (date.Month == value.Month)
                    {
                        yield return (date + value.TimeOfDay)
                            .InZoneLeniently(value.Zone);
                    }
                }
            }
        }
        else
        {
            foreach (var value in ByMonth(seed))
            {
                // Value represents week only
                var weekYear = weekYearRule.GetWeekYear(value.Date);
                var week = weekYearRule.GetWeekOfWeekYear(value.Date);

                foreach (var day in normalizedDays)
                {
                    var date = weekYearRule.GetLocalDate(weekYear, week, day);
                    yield return (date + value.TimeOfDay)
                        .InZoneLeniently(value.Zone);
                }
            }
        }
    }

    public IEnumerable<ZonedDateTime> ExpandDay(IEnumerable<ZonedDateTime> seed)
    {
        var normalizedDays = GetNormalizeDays();

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

    private List<IsoDayOfWeek> GetNormalizeDays()
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
        foreach (var value in ByWeekNo(seed))
        {
            var daysWithoutOffset = rule.ByDay
                .Where(x => x.Offset == null)
                .Select(x => x.DayOfWeek.ToIsoDayOfWeek())
                .OrderBy(x => x)
                .ToList();

            var daysWithOffset = rule.ByDay.Where(x => x.Offset.HasValue).ToList();

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
                var offsetResults = GetDayOfWeekWithOffset(start, end, daysWithOffset)
                    .OrderBy(x => x);

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
            var daysInMonth = CalendarSystem.Iso.GetDaysInMonth(value.Year, value.Month);

            var normalizedDays = rule.ByMonthDay
                .Select(monthDay => (monthDay > 0) ? monthDay : (daysInMonth + monthDay + 1))
                .Where(day => day > 0 && day <= daysInMonth)
                .OrderBy(day => day)
                .ToList();

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
        foreach (var value in ByWeekNo(seed))
        {
            var daysInMonth = CalendarSystem.Iso.GetDaysInMonth(value.Year, value.Month);

            var normalizedDays = rule.ByMonthDay
                .Select(monthDay => (monthDay > 0) ? monthDay : (daysInMonth + monthDay + 1))
                .Where(day => day > 0 && day <= daysInMonth)
                .OrderBy(day => day)
                .ToList();

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
        foreach (var value in ByWeekNo(seed))
        {
            var daysInYear = CalendarSystem.Iso.GetDaysInYear(value.Year);
            var normalizedYearDays = rule.ByYearDay
                .Select(yearDay => yearDay > 0 ? yearDay : (daysInYear + yearDay + 1))
                .ToList();

            if (normalizedYearDays.Contains(value.DayOfYear))
            {
                yield return value;
            }
        }
    }

    private IEnumerable<ZonedDateTime> ExpandYearDay(IEnumerable<ZonedDateTime> seed)
    {
        foreach (var value in ByWeekNo(seed))
        {
            var daysInYear = CalendarSystem.Iso.GetDaysInYear(value.Year);
            var normalizedYearDays = rule.ByYearDay
                .Select(yearDay => yearDay > 0 ? yearDay : (daysInYear + yearDay + 1))
                .ToList();

            var valueWeekNo = weekYearRule.GetWeekOfWeekYear(value.Date);

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
        // Hold day of the week
        var referenceDayOfWeek = zonedReferenceDate.DayOfWeek;
        var referenceWeekNo = weekYearRule.GetWeekOfWeekYear(zonedReferenceDate.Date);

        foreach (var value in ByMonth(seed))
        {
            var weekYear = weekYearRule.GetWeekYear(value.Date);
            var valueWeekNo = weekYearRule.GetWeekOfWeekYear(value.Date);

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

            var weeksInWeekYear = weekYearRule.GetWeeksInWeekYear(weekYear);

            var normalizedWeeks = rule.ByWeekNo
                .Select(weekNo => weekNo >= 0 ? weekNo : weeksInWeekYear + weekNo + 1)
                .OrderBy(weekNo => weekNo)
                .ToList();

            foreach (var weekNo in normalizedWeeks)
            {
                if (weekNo > weeksInWeekYear)
                {
                    continue;
                }

                var result = weekYearRule.GetLocalDate(weekYear, weekNo, referenceDayOfWeek);

                // If BYMONTH, verify month is the same
                if (rule.ByMonth.Count > 0 && value.Month != result.Month)
                {
                    continue;
                }

                yield return result.At(value.TimeOfDay).InZoneLeniently(value.Zone);
            }
        }
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
        foreach (var value in seed)
        {
            foreach (var month in rule.ByMonth)
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

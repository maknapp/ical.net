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
    private readonly ByRuleValues rule;
    private readonly CalDateTime referenceDate;
    private readonly DateTimeZone timeZone;
    private readonly Instant? periodStart;
    private readonly EvaluationOptions? options;

    private readonly int? count;
    private readonly int interval;
    private readonly Instant? until;
    private readonly FrequencyType frequency;
    private readonly IsoDayOfWeek firstDayOfWeek;

    private readonly IWeekYearRule weekYearRule;
    private readonly ZonedDateTime zonedReferenceDate;
    private readonly int referenceWeekNo;

    private ZonedDateTime seed;
    private int unmatchedIncrementCount = 0;

    public RecurrencePatternEvaluator2(
        RecurrencePattern pattern,
        CalDateTime referenceDate,
        DateTimeZone timeZone,
        Instant? periodStart,
        EvaluationOptions? options)
    {
        this.referenceDate = referenceDate;
        this.timeZone = timeZone;
        this.periodStart = periodStart;
        this.options = options;

        // Copy pattern values
        frequency = pattern.Frequency;
        until = pattern.Until?.ToZonedDateTime(timeZone).ToInstant();
        count = pattern.Count;
        interval = Math.Max(1, pattern.Interval);
        rule = new(pattern);

        firstDayOfWeek = pattern.FirstDayOfWeek.ToIsoDayOfWeek();
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
        seed = zonedReferenceDate;
        if (periodStart != null)
        {
            seed = SkipToByInterval(seed, periodStart.Value.InZone(seed.Zone));
        }

        // Instant that all results must be greater or equal to
        var startInstant = zonedReferenceDate.ToInstant();

        foreach (var value in BySetPosition())
        {
            var valueInstant = value.ToInstant();
            if (valueInstant > until)
            {
                break;
            }

            if (valueInstant < startInstant)
            {
                continue;
            }

            yield return new(value);

            if (++count >= this.count)
            {
                yield break;
            }

            unmatchedIncrementCount = 0;
        }
    }


    /// <summary>
    /// Get the datetime before or equal to the limit datetime
    /// using the recurrence interval.
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
        switch (frequency)
        {
            case FrequencyType.Secondly:
                time = NodaTime.Duration.FromSeconds(FromInterval((long) diff.TotalSeconds));
                return seed.Plus(time);
            case FrequencyType.Minutely:
                time = NodaTime.Duration.FromMinutes(FromInterval((long) diff.TotalMinutes));
                return seed.Plus(time);
            case FrequencyType.Hourly:
                time = NodaTime.Duration.FromHours(FromInterval((long) diff.TotalHours));
                return seed.Plus(time);
        }

        // Handle nominal
        NodaTime.Period nominalDiff;
        int nominalByInterval;
        switch (frequency)
        {
            case FrequencyType.Daily:
                nominalDiff = NodaTime.Period.Between(seed.Date, limit.Date, PeriodUnits.Days);
                nominalByInterval = FromIntervalInt(nominalDiff.Days);
                return seed.LocalDateTime.PlusDays(nominalByInterval)
                    .InZoneLeniently(seed.Zone);
            case FrequencyType.Weekly:
                nominalDiff = NodaTime.Period.Between(seed.Date, limit.Date, PeriodUnits.Weeks);
                nominalByInterval = FromIntervalInt(nominalDiff.Weeks);
                return seed.LocalDateTime.PlusWeeks(nominalByInterval)
                    .InZoneLeniently(seed.Zone);
            case FrequencyType.Monthly:
                nominalDiff = NodaTime.Period.Between(seed.Date, limit.Date, PeriodUnits.Months);
                nominalByInterval = FromIntervalInt(nominalDiff.Months);
                return seed.Date.PlusMonths(nominalByInterval)
                    .AtNearestDayOfMonth(zonedReferenceDate.Day)
                    .At(seed.TimeOfDay)
                    .InZoneLeniently(seed.Zone);
            case FrequencyType.Yearly:
                nominalDiff = NodaTime.Period.Between(seed.Date, limit.Date, PeriodUnits.Years);
                nominalByInterval = FromIntervalInt(nominalDiff.Years);
                return seed.Date.PlusYears(nominalByInterval)
                    .AtNearestDayOfMonth(zonedReferenceDate.Day)
                    .At(seed.TimeOfDay)
                    .InZoneLeniently(seed.Zone);
        }

        throw new EvaluationException("Invalid frequency type");

        long FromInterval(long value) => interval * (value / interval);

        int FromIntervalInt(int value) => interval * (value / interval);
    }

    private IEnumerable<ZonedDateTime> BySetPosition()
    {
        if (rule.BySetPosition)
        {
            return LimitSetPosition();
        }
        else
        {
            return StartByRules();
        }
    }

    private IEnumerable<ZonedDateTime> LimitSetPosition()
    {
        if (rule.HasNegativeSetPos)
        {
            while (true)
            {
                // Evaluate the entire set so that negative offsets can be handled
                var recurrenceSet = StartByRules().ToList();

                var setPos = rule.GetSetPos(recurrenceSet.Count);

                // Yield the values from each set position
                foreach (var pos in setPos)
                {
                    var setIndex = pos - 1;

                    if (setIndex >= recurrenceSet.Count)
                    {
                        break;
                    }

                    yield return recurrenceSet[setIndex];
                }

                seed = IncrementByFrequency(seed);
            }
        }
        else
        {
            var setPos = rule.GetSetPos();

            while (true)
            {
                var recurrenceSet = StartByRules();

                foreach (var value in FilterByIndex(recurrenceSet, setPos))
                {
                    yield return value;
                }

                seed = IncrementByFrequency(seed);
            }
        }

        static IEnumerable<ZonedDateTime> FilterByIndex(IEnumerable<ZonedDateTime> values, int[] setPos)
        {
            var x = 0;
            var valueEnumerator = values.GetEnumerator();

            foreach (var pos in setPos)
            {
                // Move to the next position in the set
                do
                {
                    if (!valueEnumerator.MoveNext())
                    {
                        yield break;
                    }
                } while (++x < pos);

                // Set position reached
                yield return valueEnumerator.Current;
            }
        }
    }

    private IEnumerable<ZonedDateTime> StartByRules()
    {
        if (referenceDate.HasTime)
        {
            return BySecond();
        }
        else
        {
            return ByDay();
        }
    }

    private IEnumerable<ZonedDateTime> BySecond()
    {
        if (!rule.BySecond)
        {
            return ByMinute();
        }
        else if (frequency > FrequencyType.Secondly)
        {
            return ExpandSecond();
        }
        else
        {
            // BySecond always limits
            return LimitSecond();
        }
    }

    private IEnumerable<ZonedDateTime> LimitSecond()
    {
        foreach (var value in ByMinute())
        {
            if (rule.Seconds.Contains(value.Second))
            {
                yield return value;
            }
        }
    }

    private IEnumerable<ZonedDateTime> ExpandSecond()
    {
        foreach (var value in ByMinute())
        {
            foreach (var second in rule.Seconds)
            {
                yield return value.Date
                    .At(new LocalTime(value.Hour, value.Minute, second))
                    .InZoneRelativeTo(value);
            }
        }
    }

    private IEnumerable<ZonedDateTime> ByMinute()
    {
        if (!rule.ByMinute)
        {
            return ByHour();
        }
        else if (frequency > FrequencyType.Minutely)
        {
            return ExpandMinute();
        }
        else
        {
            return LimitMinute();
        }
    }

    private IEnumerable<ZonedDateTime> LimitMinute()
    {
        foreach (var value in ByHour())
        {
            if (rule.Minutes.Contains(value.Minute))
            {
                yield return value;
            }
        }
    }

    private IEnumerable<ZonedDateTime> ExpandMinute()
    {
        foreach (var value in ByHour())
        {
            foreach (var minute in rule.Minutes)
            {
                yield return value.Date
                    .At(new LocalTime(value.Hour, minute, value.Second))
                    .InZoneRelativeTo(value);
            }
        }
    }

    private IEnumerable<ZonedDateTime> ByHour()
    {
        if (!rule.ByHour)
        {
            return ByDay();
        }
        else if (frequency > FrequencyType.Hourly)
        {
            return ExpandHour();
        }
        else
        {
            return LimitHour();
        }
    }


    private IEnumerable<ZonedDateTime> LimitHour()
    {
        foreach (var value in ByDay())
        {
            if (rule.Hours.Contains(value.Hour))
            {
                yield return value;
            }
        }
    }

    private IEnumerable<ZonedDateTime> ExpandHour()
    {
        foreach (var value in ByDay())
        {
            foreach (var hour in rule.Hours)
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
    private IEnumerable<ZonedDateTime> ByDay()
    {
        if (!rule.ByDay)
        {
            if ((frequency == FrequencyType.Monthly
                || frequency == FrequencyType.Yearly)
                && !rule.ByMonthDay
                && !rule.ByYearDay
                && !rule.ByWeekNo)
            {
                // These values represent months,
                // so they must be set to the reference day.
                return LimitDayByReferenceDay();
            }
            else
            {
                return ByMonthDay();
            }
        }
        else if (frequency == FrequencyType.Weekly)
        {
            return ExpandDayFromWeek();
        }
        else if (frequency == FrequencyType.Monthly)
        {
            if (rule.ByMonthDay)
            {
                return LimitDayOfMonth();
            }
            else
            {
                return ExpandDayFromContext(FrequencyType.Monthly);
            }
        }
        else if (frequency == FrequencyType.Yearly)
        {
            if (rule.ByYearDay || rule.ByMonthDay)
            {
                if (rule.ByMonth)
                {
                    return LimitDayOfMonth();
                }
                else
                {
                    return LimitDayOfYear();
                }
            }
            else if (rule.ByWeekNo)
            {
                return ExpandDayFromContext(FrequencyType.Weekly);
            }
            else if (rule.ByMonth)
            {
                return ExpandDayFromContext(FrequencyType.Monthly);
            }
            else
            {
                return ExpandDayFromContext(FrequencyType.Yearly);
            }
        }
        else
        {
            return LimitDayOfWeek();
        }
    }

    private IEnumerable<ZonedDateTime> LimitDayByReferenceDay()
    {
        var referenceDay = zonedReferenceDate.Day;
        foreach (var value in ByMonthDay())
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
    private IEnumerable<ZonedDateTime> LimitDayOfWeek()
    {
        if (rule.HasByDayOffsets)
        {
            throw new EvaluateException($"BYDAY offsets are not supported in {frequency}");
        }

        var daysOfWeek = rule.DaysOfWeekWithoutOffset;

        foreach (var value in ByMonthDay())
        {
            if (daysOfWeek.Contains(value.DayOfWeek))
            {
                yield return value;
            }
        }
    }

    private IEnumerable<ZonedDateTime> LimitDayOfMonth()
    {
        foreach (var value in ByMonthDay())
        {
            if (rule.MatchesByDayOfMonth(value.Date))
            {
                yield return value;
            }
        }
    }

    private IEnumerable<ZonedDateTime> LimitDayOfYear()
    {
        foreach (var value in ByMonthDay())
        {
            if (rule.MatchesByDayOfYear(value.Date))
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
    private IEnumerable<ZonedDateTime> ExpandDayFromWeek()
    {
        // The value represents a week OR a week within the month
        foreach (var value in ByMonth())
        {
            var weekYear = weekYearRule.GetWeekYear(value.Date);
            var week = weekYearRule.GetWeekOfWeekYear(value.Date);

            foreach (var day in rule.DaysOfWeekWithoutOffset)
            {
                var date = weekYearRule.GetLocalDate(weekYear, week, day);

                // Only produce date within the same month, if required
                if (!rule.ByMonth || date.Month == value.Month)
                {
                    yield return date.At(value.TimeOfDay).InZoneLeniently(value.Zone);
                }
            }
        }
    }

    private IEnumerable<ZonedDateTime> ExpandDayFromContext(FrequencyType expandFrequency)
    {
        var daysWithoutOffset = rule.DaysOfWeekWithoutOffset;

        foreach (var value in ByWeekNo())
        {
            LocalDate start, end;
            if (expandFrequency == FrequencyType.Weekly)
            {
                var weekYear = weekYearRule.GetWeekYear(value.Date);
                var weekNo = weekYearRule.GetWeekOfWeekYear(value.Date);
                start = weekYearRule.GetLocalDate(weekYear, weekNo, firstDayOfWeek);
                end = start.PlusWeeks(1);

                if (rule.ByMonth)
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

            if (rule.HasByDayOffsets)
            {
                // Offsets list specific days, so it is more efficient
                // to just calculate all of the days and then sort instead
                // of sorting the offsets by year or month.
                var offsetResults = GetDayOfWeekWithOffset(start, end, rule.DaysOfWeekWithOffset)
                    .ToArray();

                Array.Sort(offsetResults);

                if (daysWithoutOffset.Length == 0)
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
        IsoDayOfWeek[] weekDays)
    {
        if (weekDays.Length == 0)
        {
            yield break;
        }

        var value = start;

        // Get the first date that matches a week day
        var i = Array.IndexOf(weekDays, value.DayOfWeek);
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

            i = (i + 1) % weekDays.Length;
            value = value.Next(weekDays[i]);
        }
    }

    private static IEnumerable<LocalDate> GetDayOfWeekWithOffset(
        LocalDate start,
        LocalDate end,
        WeekDayValue[] weekDays)
    {
        foreach (var weekDay in weekDays)
        {
            if (weekDay.Offset > 0)
            {
                var result = start;
                if (result.DayOfWeek != weekDay.DayOfWeek)
                {
                    result = result.Next(weekDay.DayOfWeek);
                }

                result = result.PlusWeeks(weekDay.Offset.Value - 1);

                if (result < end)
                {
                    yield return result;
                }
            }
            else if (weekDay.Offset < 0)
            {
                // Context end is exclusive, so move directly
                // to previous day of week
                var result = end
                    .Previous(weekDay.DayOfWeek)
                    .PlusWeeks(weekDay.Offset.Value + 1);

                if (result >= start)
                {
                    yield return result;
                }
            }
            else
            {
                throw new EvaluationException("Encountered a day offset of 0 which is not allowed.");
            }
        }
    }

    private IEnumerable<ZonedDateTime> ByMonthDay()
    {
        if (!rule.ByMonthDay)
        {
            return ByYearDay();
        }
        else if (frequency == FrequencyType.Weekly)
        {
            throw new EvaluationException("BYMONTHDAY is not supported in WEEKLY");
        }
        else if (frequency > FrequencyType.Weekly)
        {
            if (rule.ByYearDay)
            {
                // Values will be days, so just limit
                return LimitMonthDay();
            }
            else if (rule.ByWeekNo)
            {
                return ExpandMonthDayFromWeek();
            }
            else
            {
                return ExpandMonthDayFromMonth();
            }
        }
        else
        {
            return LimitMonthDay();
        }

    }

    private IEnumerable<ZonedDateTime> LimitMonthDay()
    {
        foreach (var value in ByYearDay())
        {
            if (rule.GetMonthDays(value.Year, value.Month).Contains(value.Day))
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// Expand by day of month from months.
    /// Supports MONTHLY, YEARLY, and YEARLY+ByMonth.
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    private IEnumerable<ZonedDateTime> ExpandMonthDayFromMonth()
    {
        foreach (var value in ByMonth())
        {
            foreach (var day in rule.GetMonthDays(value.Year, value.Month))
            {
                yield return new LocalDate(value.Year, value.Month, day)
                    .At(value.TimeOfDay)
                    .InZoneLeniently(value.Zone);
            }
        }
    }

    /// <summary>
    /// Expand by day of month from week.
    /// Supports YEARLY+BYWEEKNO and YEARLY+BYMONTH+BYWEEKNO.
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    private IEnumerable<ZonedDateTime> ExpandMonthDayFromWeek()
    {
        foreach (var value in ByWeekNo())
        {
            var monthDays = rule.GetMonthDays(value.Year, value.Month);

            var weekYear = weekYearRule.GetWeekYear(value.Date);
            var weekNo = weekYearRule.GetWeekOfWeekYear(value.Date);

            var result = weekYearRule.GetLocalDate(weekYear, weekNo, firstDayOfWeek);
            var end = result.PlusWeeks(1);

            do
            {
                if (monthDays.Contains(result.Day)
                    && (rule.Months.Length == 0 || rule.Months.Contains(result.Month)))
                {
                    yield return result.At(value.TimeOfDay).InZoneLeniently(value.Zone);
                }
            } while ((result = result.PlusDays(1)) < end);
        }
    }

    private IEnumerable<ZonedDateTime> ByYearDay()
    {
        if (!rule.ByYearDay)
        {
            return ByWeekNo();
        }
        else if (frequency == FrequencyType.Yearly)
        {
            return ExpandYearDay();
        }
        else if (frequency < FrequencyType.Daily)
        {
            return LimitYearDay();
        }
        else
        {
            throw new EvaluationException($"BYYEARDAY is not supported with {frequency}");
        }
    }

    private IEnumerable<ZonedDateTime> LimitYearDay()
    {
        foreach (var value in ByWeekNo())
        {
            if (rule.GetYearDays(value.Year).Contains(value.DayOfYear))
            {
                yield return value;
            }
        }
    }

    private IEnumerable<ZonedDateTime> ExpandYearDay()
    {
        foreach (var value in ByWeekNo())
        {
            var valueWeekNo = weekYearRule.GetWeekOfWeekYear(value.Date);

            foreach (var day in rule.GetYearDays(value.Year))
            {
                var result = new LocalDate(value.Year, 1, 1).PlusDays(day - 1);

                // Ignore values outside of the calendar year
                if (result.Year != value.Year)
                {
                    continue;
                }

                // Limit by month if specified
                if (rule.ByMonth && result.Month != value.Month)
                {
                    continue;
                }

                // Limit by weekNo is specified
                if (rule.ByWeekNo)
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

    private IEnumerable<ZonedDateTime> ByWeekNo()
    {
        if (!rule.ByWeekNo)
        {
            return ByMonth();
        }
        else if (frequency == FrequencyType.Yearly)
        {
            return ExpandWeekNo();
        }
        else
        {
            throw new EvaluationException($"BYWEEKNO is not supported with {frequency}");
        }
    }

    /// <summary>
    /// Expands yearly week
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    private IEnumerable<ZonedDateTime> ExpandWeekNo()
    {
        foreach (var value in ByMonth())
        {
            var weekYear = GetWeekYearBasedOnReferenceWeekYear(value.Date);

            var weeksInWeekYear = weekYearRule.GetWeeksInWeekYear(weekYear);

            foreach (var weekNo in rule.GetWeeks(weeksInWeekYear))
            {
                if (weekNo > weeksInWeekYear)
                {
                    continue;
                }

                var result = weekYearRule.GetLocalDate(weekYear, weekNo, zonedReferenceDate.DayOfWeek);

                // If BYMONTH, verify month is the same
                if (rule.ByMonth && value.Month != result.Month)
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

    private IEnumerable<ZonedDateTime> ByMonth()
    {
        if (!rule.ByMonth)
        {
            return Expand();
        }
        else if (frequency == FrequencyType.Yearly)
        {
            return ExpandMonth();
        }
        else
        {
            return LimitMonth();
        }
    }

    private IEnumerable<ZonedDateTime> LimitMonth()
    {
        foreach (var value in Expand())
        {
            if (rule.Months.Contains(value.Month))
            {
                yield return value;
            }
        }
    }

    private IEnumerable<ZonedDateTime> ExpandMonth()
    {
        foreach (var value in Expand())
        {
            foreach (var month in rule.Months)
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

    private IEnumerable<ZonedDateTime> Expand()
    {
        if (rule.BySetPosition)
        {
            // Yield only one value so that a single set can be generated
            yield return seed;
        }
        else
        {
            // Expand forever
            while (true)
            {
                yield return seed;
                seed = IncrementByFrequency(seed);
            }
        }
    }

    private ZonedDateTime IncrementByFrequency(ZonedDateTime seed)
    {
        if (unmatchedIncrementCount++ > options?.MaxUnmatchedIncrementsLimit)
        {
            throw new EvaluationLimitExceededException();
        }

        return frequency switch
        {
            FrequencyType.Secondly => seed.PlusSeconds(interval),
            FrequencyType.Minutely => seed.PlusMinutes(interval),
            FrequencyType.Hourly => seed.PlusHours(interval),

            FrequencyType.Daily => seed.LocalDateTime
                .PlusDays(interval)
                .InZoneLeniently(seed.Zone),

            FrequencyType.Weekly => seed.LocalDateTime
                .PlusWeeks(interval)
                .InZoneLeniently(seed.Zone),

            FrequencyType.Monthly => seed.Date
                .PlusMonths(interval)
                .AtNearestDayOfMonth(zonedReferenceDate.Day)
                .At(seed.TimeOfDay)
                .InZoneLeniently(seed.Zone),

            FrequencyType.Yearly => seed.Date
                .PlusYears(interval)
                .AtNearestDayOfMonth(zonedReferenceDate.Day)
                .At(seed.TimeOfDay)
                .InZoneLeniently(seed.Zone),

            _ => throw new EvaluateException("Invalid frequency")
        };
    }
}

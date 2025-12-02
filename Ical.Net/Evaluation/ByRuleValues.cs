//
// Copyright ical.net project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;

namespace Ical.Net.Evaluation;

internal class ByRuleValues
{
    private readonly IsoDayOfWeek firstDayOfWeek;
    private DayOfWeekComparer? dayOfWeekComparer;

    private readonly int[] normalMonths;

    private readonly int[] normalHours;
    private readonly int[] normalMinutes;
    private readonly int[] normalSeconds;

    private readonly int[] byWeekNo;
    private readonly NormalValues<int, int> weeks;

    private readonly int[] byYearDay;
    private readonly NormalValues<int, int> yearDays;

    private readonly int[] byMonthDay;
    private readonly NormalValues<(int Year, int Month), int> monthDays;

    private readonly WeekDayValue[] byDay;
    private IsoDayOfWeek[]? normalDaysOfWeek;
    private WeekDayValue[]? daysOfWeekWithOffset;

    private readonly int[] bySetPos;
    private readonly NormalValues<int, int> setPos;

    public ByRuleValues(RecurrencePattern rule)
    {
        firstDayOfWeek = rule.FirstDayOfWeek.ToIsoDayOfWeek();

        normalMonths = SortValues(rule.ByMonth);

        normalHours = SortValues(rule.ByHour);
        normalMinutes = SortValues(rule.ByMinute);
        normalSeconds = SortValues(rule.BySecond);

        byWeekNo = [.. rule.ByWeekNo];
        weeks = new();

        // If all values are non-negative, just copy and sort once
        if (byWeekNo.All(static x => x >= 0))
        {
            var values = new int[byWeekNo.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = rule.ByWeekNo[i];
            }
            Array.Sort(values);
            weeks.Values = values;
            weeks.AlwaysNormal = true;
        }


        byYearDay = [.. rule.ByYearDay];
        yearDays = new();

        // If all values are positive, just copy and sort once
        if (byYearDay.All(static x => x > 0))
        {
            var values = new int[byYearDay.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = rule.ByYearDay[i];
            }
            Array.Sort(values);
            yearDays.Values = values;
            yearDays.AlwaysNormal = true;
        }

        byMonthDay = [.. rule.ByMonthDay];
        monthDays = new();

        byDay = [.. rule.ByDay.Select(static x => new WeekDayValue(x))];
        HasByDayOffsets = byDay.Any(static x => x.Offset != null);

        bySetPos = [.. rule.BySetPosition];
        setPos = new();

        // If all values are positive, just copy and sort once
        HasNegativeSetPos = bySetPos.Any(static x => x < 0);
        if (!HasNegativeSetPos)
        {
            setPos.Values = SortValues(rule.BySetPosition);
            setPos.AlwaysNormal = true;
        }
    }

    public int[] Months => normalMonths;

    public int[] Hours => normalHours;

    public int[] Minutes => normalMinutes;

    public int[] Seconds => normalSeconds;

    public bool HasByDayOffsets { get; }

    public bool HasNegativeSetPos { get; }

    public bool ByMonth => normalMonths.Length > 0;

    public bool ByWeekNo => byWeekNo.Length > 0;

    public bool ByYearDay => byYearDay.Length > 0;

    public bool ByMonthDay => byMonthDay.Length > 0;

    public bool ByDay => byDay.Length > 0;

    public bool ByHour => normalHours.Length > 0;

    public bool ByMinute => normalMinutes.Length > 0;

    public bool BySecond => normalSeconds.Length > 0;

    public bool BySetPosition => bySetPos.Length > 0;

    /// <summary>
    /// Normalized days of the week that did not have an offset value.
    /// </summary>
    public IsoDayOfWeek[] DaysOfWeekWithoutOffset
    {
        get
        {
            dayOfWeekComparer ??= new DayOfWeekComparer(firstDayOfWeek);
            normalDaysOfWeek ??= NormalizeDaysWithoutOffset(byDay, dayOfWeekComparer);
            return normalDaysOfWeek;
        }
    }

    public WeekDayValue[] DaysOfWeekWithOffset
    {
        get
        {
            // The purpose of ordering is to get the resulting
            // dates more ordered. This is not normalized.
            daysOfWeekWithOffset ??= byDay
                .Where(x => x.Offset != null)
                .OrderBy(x => x.Offset)
                .ToArray();

            return daysOfWeekWithOffset;
        }
    }

    public int[] GetWeeks(int weeksInWeekYear)
    {
        if (weeks.Values is null || (!weeks.AlwaysNormal && weeks.Key != weeksInWeekYear))
        {
            weeks.Values ??= new int[byWeekNo.Length];
            NormalizeWeekNo(byWeekNo, weeksInWeekYear, weeks.Values);
            weeks.Key = weeksInWeekYear;
        }

        return weeks.Values;
    }

    public int[] GetYearDays(int year)
    {
        if (yearDays.Values is null || (!yearDays.AlwaysNormal && yearDays.Key != year))
        {
            yearDays.Values ??= new int[byYearDay.Length];
            NormalizeYearDays(byYearDay, year, yearDays.Values);
            yearDays.Key = year;
        }

        return yearDays.Values;
    }

    public int[] GetMonthDays(int year, int month)
    {
        if (monthDays.Values is null || (!monthDays.AlwaysNormal && monthDays.Key != (year, month)))
        {
            monthDays.Values = NormalizeMonthDay(byMonthDay, year, month);
            monthDays.Key = (year, month);
        }

        return monthDays.Values;
    }

    public int[] GetSetPos(int count = 0)
    {
        if (setPos.Values is null || (!setPos.AlwaysNormal && setPos.Key != count))
        {
            setPos.Values ??= new int[bySetPos.Length];
            NormalizeSetPos(bySetPos, count, setPos.Values);
            setPos.Key = count;
        }

        return setPos.Values;
    }

    public bool MatchesByDayOfMonth(LocalDate value)
    {
        foreach (var weekDay in byDay)
        {
            if (MatchesWeekDay(weekDay, value))
            {
                return true;
            }
        }

        return false;

        static bool MatchesWeekDay(WeekDayValue weekDay, LocalDate value)
        {
            if (weekDay.DayOfWeek != value.DayOfWeek)
            {
                return false;
            }

            if (weekDay.Offset == null)
            {
                return true;
            }

            // Check if offset matches
            if (weekDay.Offset > 0)
            {
                var offsetDate = new LocalDate(value.Year, value.Month, 1);
                if (offsetDate.DayOfWeek != weekDay.DayOfWeek)
                {
                    offsetDate = offsetDate.Next(weekDay.DayOfWeek);
                }

                offsetDate = offsetDate.PlusWeeks(weekDay.Offset.Value - 1);

                return offsetDate == value;
            }
            else if (weekDay.Offset < 0)
            {
                var offsetDate = new LocalDate(value.Year, value.Month, 1)
                    .PlusMonths(1)
                    .Previous(weekDay.DayOfWeek)
                    .PlusWeeks(weekDay.Offset.Value + 1);

                return offsetDate == value;
            }
            else
            {
                throw new EvaluateException("Encountered a day offset of 0 which is not allowed.");
            }
        }
    }

    public bool MatchesByDayOfYear(LocalDate value)
    {
        foreach (var weekDay in byDay)
        {
            if (MatchesWeekDay(weekDay, value))
            {
                return true;
            }
        }

        return false;

        static bool MatchesWeekDay(WeekDayValue weekDay, LocalDate value)
        {
            if (weekDay.DayOfWeek != value.DayOfWeek)
            {
                return false;
            }

            if (weekDay.Offset == null)
            {
                return true;
            }

            // Check if offset matches
            if (weekDay.Offset > 0)
            {
                var offsetDate = new LocalDate(value.Year, 1, 1);
                if (offsetDate.DayOfWeek != weekDay.DayOfWeek)
                {
                    offsetDate = offsetDate.Next(weekDay.DayOfWeek);
                }

                offsetDate = offsetDate.PlusWeeks(weekDay.Offset.Value - 1);

                return offsetDate == value;
            }
            else if (weekDay.Offset < 0)
            {
                var offsetDate = new LocalDate(value.Year, 1, 1)
                    .PlusYears(1)
                    .Previous(weekDay.DayOfWeek)
                    .PlusWeeks(weekDay.Offset.Value + 1);

                return offsetDate == value;
            }
            else
            {
                throw new EvaluateException("Encountered a day offset of 0 which is not allowed.");
            }
        }
    }

    private static int[] SortValues(List<int> byRule)
    {
        int[] values = [.. byRule];
        Array.Sort(values);
        return values;
    }

    private static void NormalizeWeekNo(int[] byWeekNo, int weeksInWeekYear, int[] normalizedWeeks)
    {
        for (var i = 0; i < normalizedWeeks.Length; i++)
        {
            var weekNo = byWeekNo[i];
            normalizedWeeks[i] = (weekNo >= 0) ? weekNo : weeksInWeekYear + weekNo + 1;
        }
        Array.Sort(normalizedWeeks);
    }

    private static void NormalizeYearDays(int[] yearDays, int year, int[] normalizedYearDays)
    {
        var daysInYear = CalendarSystem.Iso.GetDaysInYear(year);
        for (var i = 0; i < normalizedYearDays.Length; i++)
        {
            var yearDay = yearDays[i];
            normalizedYearDays[i] = (yearDay > 0) ? yearDay : (daysInYear + yearDay + 1);
        }
        Array.Sort(normalizedYearDays);
    }

    private static int[] NormalizeMonthDay(int[] byMonthDay, int year, int month)
    {
        var daysInMonth = CalendarSystem.Iso.GetDaysInMonth(year, month);

        var normalizedMonthDays = byMonthDay
            .Select(monthDay => (monthDay > 0) ? monthDay : (daysInMonth + monthDay + 1))
            .Where(day => day > 0 && day <= daysInMonth)
            .ToArray();

        Array.Sort(normalizedMonthDays);

        return normalizedMonthDays;
    }

    private static IsoDayOfWeek[] NormalizeDaysWithoutOffset(WeekDayValue[] byDay, DayOfWeekComparer dayOfWeekComparer)
    {
        var normalizedDays = byDay
            .Where(x => x.Offset == null)
            .Select(x => x.DayOfWeek)
            .ToArray();

        Array.Sort(normalizedDays, dayOfWeekComparer);

        return normalizedDays;
    }

    private static void NormalizeSetPos(int[] bySetPos, int count, int[] normalizedSetPos)
    {
        for (var i = 0; i < normalizedSetPos.Length; i++)
        {
            var setPos = bySetPos[i];
            normalizedSetPos[i] = (setPos < 0) ? setPos + count + 1 : setPos;
        }
        Array.Sort(normalizedSetPos);
    }

    private class NormalValues<T, R> where T: struct, IComparable<T> where R : struct
    {
        public T Key { get; set; }

        public R[]? Values { get; set; }

        public bool AlwaysNormal { get; set; }
    }

    /// <summary>
    /// Compares day of week according to the
    /// specified first day of the week.
    /// </summary>
    /// <param name="firstDayOfWeek"></param>
    private class DayOfWeekComparer(IsoDayOfWeek firstDayOfWeek) : IComparer<IsoDayOfWeek>
    {
        private const int max = (int)IsoDayOfWeek.Sunday;

        public int Compare(IsoDayOfWeek x, IsoDayOfWeek y)
            => DayValue(x).CompareTo(DayValue(y));

        private int DayValue(IsoDayOfWeek value)
            => (value + max - firstDayOfWeek) % max;
    }
}

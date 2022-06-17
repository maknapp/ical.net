﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Ical.Net;
using Ical.Net.DataTypes;

namespace PerfTests
{
    public class ApplicationWorkflows
    {
        private static readonly TimeSpan _oneYear = TimeSpan.FromDays(365);
        private static readonly DateTime _searchStart = DateTime.Now.Subtract(_oneYear);
        private static readonly DateTime _searchEnd = DateTime.Now.Add(_oneYear);
        private static readonly List<string> _manyCalendars = GetIcalStrings();

        private static List<string> GetIcalStrings()
        {
            var calendarPath = @"Ical.Net.CoreUnitTests\Calendars";
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var path = Path.GetFullPath(Path.Combine(currentDirectory, "..", "..", "..", ".."));
            var topLevelIcsPath = Path.GetFullPath(Path.Combine(path, calendarPath));

            // Calendar path may be in different levels, so keep searching
            while (!string.IsNullOrEmpty(Path.GetDirectoryName(path)) && !Directory.Exists(topLevelIcsPath))
            {
                path = Path.GetFullPath(Path.Combine(path, ".."));
                topLevelIcsPath = Path.GetFullPath(Path.Combine(path, calendarPath));
            }

            return Directory.EnumerateFiles(topLevelIcsPath, "*.ics", SearchOption.AllDirectories)
                .Select(File.ReadAllText)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(s => !s.Contains("InternetExplorer") && !s.Contains("SECONDLY"))
                .OrderByDescending(s => s.Length)
                .Take(10)
                .ToList();
        }

        [Benchmark]
        public List<Occurrence> SingleThreaded()
        {
            return _manyCalendars
                .SelectMany(Calendar.Load<Calendar>)
                .SelectMany(c => c.Events)
                .SelectMany(e => e.GetOccurrences(_searchStart, _searchEnd))
                .ToList();
        }

        [Benchmark]
        public List<Occurrence> ParallelUponDeserialize()
        {
            return _manyCalendars
                .AsParallel()
                .SelectMany(Calendar.Load<Calendar>)
                .SelectMany(c => c.Events)
                .SelectMany(e => e.GetOccurrences(_searchStart, _searchEnd))
                .ToList();
        }

        [Benchmark]
        public List<Occurrence> ParallelUponGetOccurrences()
        {
            return _manyCalendars
                .SelectMany(Calendar.Load<Calendar>)
                .SelectMany(c => c.Events)
                .AsParallel()
                .SelectMany(e => e.GetOccurrences(_searchStart, _searchEnd))
                .ToList();
        }

        [Benchmark]
        public List<Occurrence> ParallelDeserializeSequentialGatherEventsParallelGetOccurrences()
        {
            return _manyCalendars
                .AsParallel()
                .SelectMany(Calendar.Load<Calendar>)
                .AsSequential()
                .SelectMany(c => c.Events)
                .SelectMany(e => e.GetOccurrences(_searchStart, _searchEnd))
                .ToList();
        }
    }
}
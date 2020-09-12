using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net.DataTypes;
using NUnit.Framework;

namespace Ical.Net.FrameworkUnitTests
{
    public class CollectionHelpersTests
    {
        private static readonly DateTime Now = new DateTime(2010, 11, 12, 05, 06, 07);
        private static readonly DateTime OneDayLater = Now.AddHours(1);

        [Test]
        public void ExceptionDateTest()
        {
            Assert.AreEqual(GetExceptionDates(), GetExceptionDates());
            Assert.AreNotEqual(GetExceptionDates(), null);
            Assert.AreNotEqual(null, GetExceptionDates());

            var changedPeriod = GetExceptionDates();
            changedPeriod.First().First().StartTime = new CalDateTime(Now.AddHours(-1));

            Assert.AreNotEqual(GetExceptionDates(), changedPeriod);
        }

        private static List<PeriodList> GetExceptionDates()
        {
            return new List<PeriodList> { new PeriodList { new Period(new CalDateTime(OneDayLater.Date)) } };
        }
    }
}

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DoorDetector;
using System.Linq;


namespace DoorDetectorUnitTests
{
    [TestClass]
    public class DateHelperTests
    {
        [TestMethod]
        public void TestMonthDays()
        {
            var days = DateHelper.MonthDays(new DateTimeOffset(new DateTime(2017, 9, 1)));
            Assert.AreEqual(30, days.Count());
        }

        [TestMethod]
        public void TestGetEveryDaysInMonth()
        {
            var days = DateHelper.GetEveryDaysInMonth(new DateTime(2017, 9, 1), DayOfWeek.Monday, DayOfWeek.Thursday);
            Assert.AreEqual(8, days.Count());
            var expected = new DateTime[] {
                new DateTime(2017,9,4),
                new DateTime(2017,9,7),
                new DateTime(2017,9,11),
                new DateTime(2017,9,14),
                new DateTime(2017,9,18),
                new DateTime(2017,9,21),
                new DateTime(2017,9,25),
                new DateTime(2017,9,28),
            };
            var actual = days.ToArray();
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}

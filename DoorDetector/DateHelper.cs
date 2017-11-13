using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDetector
{
    class DateHelper
    {
        // This presumes that weeks start with Monday.
        // Week 1 is the 1st week of the year with a Thursday in it.
        public static int GetIso8601WeekOfYear(DateTime time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public static DateTime GetBeginWeekOfYear(int year, int weekNumber)
        {
            DateTime begin = new DateTime(year, 1, 1);
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(begin);
            switch (day)
            {
                case DayOfWeek.Sunday:
                    begin = begin.AddDays(1);
                    break;
                default:
                    begin = begin.AddDays(8 - (int)day);
                    break;
            }
            int firstWeek = GetIso8601WeekOfYear(begin);
            var target = begin.AddDays((weekNumber - firstWeek) * 7);
            return target;
        }

        /// <summary>
        /// liste des semaines de l'année
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static IEnumerable<int> YearWeeks(DateTimeOffset date)
        {
            return Enumerable.Range(1, GetIso8601WeekOfYear(new DateTime(date.Year, 12, 31)));
        }

        /// <summary>
        /// liste des jours du mois
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static IEnumerable<int> MonthDays(DateTimeOffset date)
        {
            return Enumerable.Range(1, DateTime.DaysInMonth(date.Year, date.Month));
        }

        /// <summary>
        /// liste des jours de la semaine spécifiée
        /// </summary>
        /// <param name="year"></param>
        /// <param name="week"></param>
        /// <returns></returns>
        public static IEnumerable<int> WeekDays(int year, int week)
        {
            var begin = GetBeginWeekOfYear(year, week);
            for (int i = 0; i < 7; i++)
            {
                yield return begin.Day;
                begin = begin.AddDays(1);
            }
        }

        public static IEnumerable<DateTime> GetEveryDaysInMonth(DateTime month, params DayOfWeek[] whichDays)
        {
            var daysofweek = whichDays;
            var days = Enumerable.Range(1, DateTime.DaysInMonth(month.Year, month.Month)).Select(day => new DateTime(month.Year, month.Month, day));

            return days.Join(whichDays, x => x.DayOfWeek, y => y, (x, y) => x);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDetector
{
    public sealed class DoorDetectorService : IDoorDetectorService
    {
        DBDoorDetector _db;

        public DoorDetectorService(IDBPathProvider pathProvider)
        {
            this._db = new DBDoorDetector(pathProvider);
        }

        public void AddBookmark(int eventId, string name)
        {
            this._db.AddBookmark(new BookmarkEvent { IdEvent = eventId, BookmarkName = name });
        }

        public void AddDoorEvent(DoorEvent ev)
        {
            this._db.AddDoorEvent(ev);
        }


        public void BackupDatabase()
        {
            this._db.BackupDatabase();
        }

        public void CheckDbStructure()
        {
            this._db.CheckDbStructure();
        }

        public IEnumerable<DoorAggregat> DoorDashboard()
        {
            return this._db.DoorDashboard();
        }

        public IEnumerable<DoorEvent> GetDoorEvents(int year, int skip, int take)
        {
            var dtStart = new DateTimeOffset(new DateTime(year, 1, 1));
            var dtEnd = dtStart.AddYears(1).AddSeconds(-1);

            if (skip >= 0 && take > 0)
            {
                return this._db.SelectBetweenSkipTake(dtStart, dtEnd, skip, take);
            }
            return this._db.SelectBetween(dtStart, dtEnd);
        }

        public IEnumerable<DoorEvent> GetDoorEventsForCurrentYear(DateTimeOffset date)
        {
            var dtStart = new DateTimeOffset(new DateTime(date.Year, 1, 1));
            var dtEnd = dtStart.AddYears(1).AddSeconds(-1);
            return this._db.SelectBetween(dtStart, dtEnd);
        }

        public IEnumerable<DoorEvent> GetDoorEventsForCurrentMonth(DateTimeOffset date)
        {
            var dt = date.Date;
            var y = dt.Year;
            var m = dt.Month;

            return GetDoorEventsForMonth(y, m);
        }

        /// <summary>
        /// get event for current week
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DoorEvent> GetDoorEventsForCurrentWeek(DateTimeOffset date)
        {
            getWeekStartAndEnd(date, out var dtweekStart, out var dtendWeek);
            return this._db.SelectBetween(dtweekStart, dtendWeek);
        }

        public IEnumerable<DoorEvent> GetDoorEventsForMondayThursday(int year, int month)
        {
            return this._db.GetDoorEventsForMondayThursday(year, month);
        }

        public IEnumerable<DoorEvent> GetDoorEventsForMonth(int year, int month)
        {
            var dt = new DateTimeOffset(new DateTime(year, month, 1));

            var end = dt.AddMonths(1).AddSeconds(-1);
            return this._db.SelectBetween(dt, end);
        }

        public IEnumerable<DoorStats> GetDoorStatsBy(DoorStatsBy by, DateTimeOffset date)
        {
            return this._db.GetDoorStatsBy(by, date);
        }

        #region private methods
        private void getWeekStartAndEnd(DateTimeOffset dateRef, out DateTimeOffset start, out DateTimeOffset end)
        {
            var dtweekStart = dateRef.Date;

            var removeDays = 0;
            switch (dtweekStart.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    removeDays = 6;
                    break;
                default:
                    removeDays = ((int)dtweekStart.DayOfWeek) - 1;
                    break;
            }

            dtweekStart = dtweekStart.AddDays(-removeDays);
            var dtendWeek = dtweekStart.AddDays(7).AddSeconds(-1);
            start = dtweekStart;
            end = dtendWeek;
        }
        #endregion
    }
}

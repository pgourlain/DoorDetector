using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace DoorDetector
{
    /// <summary>
    /// Db implementation
    /// </summary>
    internal sealed class DBDoorDetector
    {
        private static string DB_FILENAME = "dbdetector.sqlite";
        //\\192.168.1.13\c$\Data\Users\DefaultAccount\AppData\Local\Packages\DoorDetector-uwp_1h0828nbdp8n0\LocalState
        private static string DB_PATH = Path.Combine(ApplicationData.Current.LocalFolder.Path, DB_FILENAME);

        private IDBPathProvider _DbPathProvider;
        private ManualResetEventSlim _locker = new ManualResetEventSlim(true);

        public DBDoorDetector(IDBPathProvider dbPathProvider)
        {
            this._DbPathProvider = dbPathProvider;
        }

        private string DatabaseFullPath
        {
            get
            {
                if (this._DbPathProvider != null)
                {
                    return Path.Combine(this._DbPathProvider.GetDBFolderPath(), DB_FILENAME);
                }
                return DB_PATH;
            }
        }

        private async Task<bool> CheckFileExists(string fileName)
        {
            try
            {
                var store = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task BackupDbFile(string newFileName)
        {
            var dbFile = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(DB_FILENAME);
            await dbFile.CopyAsync(Windows.Storage.ApplicationData.Current.LocalFolder, newFileName);
        }

        private SqliteConnection getConnection()
        {
            var cnx = new DBSqlConnectionLocker(_locker, string.Format("Data Source={0};", this.DatabaseFullPath));
            cnx.Open();
            return cnx;
        }


        public void CheckDbStructure()
        {
            using (var cnx = getConnection())
            {
                using (var cmd = cnx.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='doorevent';";
                    if ((long)cmd.ExecuteScalar() == 0)
                    {
                        cmd.CommandText = "CREATE TABLE doorevent (num INTEGER PRIMARY KEY ASC, doorid int, eventopentime text, eventclosetime text)";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        #region generic select
        private IEnumerable<T> GenericSelect<T>(string select, Func<SqliteDataReader, IEnumerable<T>> materializeFun)
        {
            using (var cnx = getConnection())
            {
                using (var cmd = cnx.CreateCommand())
                {
                    cmd.CommandText = select;
                    using (var reader = cmd.ExecuteReader())
                    {
                        return materializeFun(reader).ToArray();
                    }
                }
            }
        }

        private IEnumerable<T> GenericSelect<T>(string select, Func<object[], IEnumerable<T>> materializeFun)
        {
            return GenericSelect(select, (SqliteDataReader reader) => MaterializeValues(reader, materializeFun));
        }
        #endregion


        #region insert
        public void AddBookmark(BookmarkEvent ev )
        {

        }

        public void AddDoorEvent(DoorEvent ev)
        {
            using (var cnx = getConnection())
            {
                var openDate = ev.Opentime.ToString("O");
                var closeDate = ev.Closetime.ToString("O");
                bool insert = true;
                //parfois il y a un rebond de porte < 0.5 secondes, il faut donc mettre à jour l'enregistrement précédent
                //et ne pas inseré de nouveau
                using (var cmd = cnx.CreateCommand())
                {
                    cmd.CommandText = string.Format("UPDATE doorevent set eventclosetime = '{0}' where (julianday('{1}') - julianday(eventclosetime)) * 24 * 60 * 60 between 0 and 0.5", closeDate, openDate);
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        insert = false;
                    }
                }
                if (insert)
                {
                    using (var cmdInsert = cnx.CreateCommand())
                    {
                        cmdInsert.CommandText = string.Format("INSERT INTO doorevent (doorid, eventopentime, eventclosetime) VALUES ({0}, '{1}', '{2}')", ev.Id, openDate, closeDate);
                        cmdInsert.ExecuteNonQuery();
                    }
                }
            }
        }

        internal void BackupDatabase()
        {
            _locker.Wait();
            _locker.Reset();
            try
            {
                var now = DateTime.Now;
                var weekNumber = DateHelper.GetIso8601WeekOfYear(now);
                var currentBackup = string.Format("{0}.backup_{1}_{2}", DB_FILENAME, now.Year, weekNumber);
                var t = CheckFileExists(currentBackup);
                if (!t.Result)
                {
                    var t1 = BackupDbFile(currentBackup);
                    t1.Wait();
                }
            }
            finally
            {
                _locker.Set();
            }
        }

        /// <summary>
        /// met a jour les évènements qui sont proches &lt; 0.5 secondes
        /// </summary>
        /// <returns></returns>
        public int UpdateClosedEvents()
        {
            return 0;
            /*
            using (var cnx = getConnection())
            {
                using (var trans = cnx.BeginTransaction())
                {
                    var Sql = @"select A.num, A.eventopentime, A.eventclosetime, B.num, B.eventopentime, B.eventclosetime, (julianday((A.eventopentime)) - julianday((B.eventclosetime)))* 24 * 60 * 60 as diff
from doorevent as A, doorevent as B

where A.num > B.num and (julianday((A.eventopentime)) - julianday((B.eventclosetime)))* 24 * 60 * 60 between 0 and 0.5";
                    trans.Commit();
                }
                return 0;
            }
            */
        }
        #endregion

        #region Select Doorevent

        public IEnumerable<DoorEvent> SelectBetween(DateTimeOffset dtStart, DateTimeOffset dtEnd)
        {
            //            var start = dtStart.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
            //            var end = dtEnd.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
            //            return SelectDoorEventAndMaterialize(string.Format(@"WHERE strftime('%Y-%m-%dT%H:%M:%S', eventopentime) BETWEEN
            // strftime('%Y-%m-%dT%H:%M:%S', '{0}')
            //AND
            // strftime('%Y-%m-%dT%H:%M:%S', '{1}')
            //ORDER by num asc", start, end));
            return SelectBetweenSkipTake(dtStart, dtEnd, 0, 1000000);
        }

        public IEnumerable<DoorEvent> SelectBetweenSkipTake(DateTimeOffset dtStart, DateTimeOffset dtEnd, int skip, int take)
        {
            var start = dtStart.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
            var end = dtEnd.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");

            return SelectDoorEventAndMaterialize(string.Format(@"WHERE strftime('%Y-%m-%dT%H:%M:%S', eventopentime) BETWEEN
 strftime('%Y-%m-%dT%H:%M:%S', '{0}')
AND
 strftime('%Y-%m-%dT%H:%M:%S', '{1}')
ORDER by num asc LIMIT {2}, {3}", start, end, skip, take));
        }

        private IEnumerable<DoorEvent> SelectDoorEventAndMaterialize(string where)
        {
            return GenericSelect("SELECT num, doorid, eventopentime, eventclosetime from doorevent " + where, this.MaterializeDoorEvent);
        }

        private IEnumerable<T> MaterializeValues<T>(SqliteDataReader reader, Func<object[], IEnumerable<T>> materializeFun)
        {
            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                foreach (var item in materializeFun(values))
                {
                    yield return item;
                }
            }
        }

        private IEnumerable<DoorEvent> MaterializeDoorEvent(object[] values)
        {
            DateTimeOffset ot, ct;
            if (!DateTimeOffset.TryParseExact((string)values[2], "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out ot))
            {
                ot = DateTimeOffset.Parse((string)values[2]);
            }
            if (!DateTimeOffset.TryParseExact((string)values[3], "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out ct))
            {
                ct = DateTimeOffset.Parse((string)values[3]);
            }
            yield return new DoorEvent
            {
                Num = (long)values[0],
                Id = (int)(long)values[1],
                Opentime = ot,
                Closetime = ct
            };
        }

        #endregion

        #region Dashboard
        private IEnumerable<DoorAggregat> byX(string namePrefix, object[] values)
        {
            yield return new DoorAggregat { AggregatName = namePrefix+"_openingcount", AggregatValue = string.Format("{0}", values[0]), Unit = "times", IdEvent=-1 };
            yield return new DoorAggregat { AggregatName = namePrefix+"_openingtime", AggregatValue = string.Format("{0}", values[2]), Unit = "hours", IdEvent=-1 };
        }

        private IEnumerable<DoorAggregat> byWeek(object[] values) => byX("week", values);

        private IEnumerable<DoorAggregat> byMonth(object[] values) => byX("month", values);
        
        private IEnumerable<DoorAggregat> byYear(object[] values) => byX("year", values);

        public IEnumerable<DoorAggregat> DoorDashboard()
        {
            var weekSql = @"select count(*) as nb, 
strftime('%W',eventopentime) as week, 
 STRFTIME('%H:%M:%f',  sum(julianday(eventclosetime) - julianday(eventopentime)) * 24 * 60 * 60, 'unixepoch') as diff 
 from doorevent where week = strftime('%W', 'now')";

            var monthSql = @"select count(*) as nb, 
strftime('%m',eventopentime) as month, 
 STRFTIME('%H:%M:%f',  sum(julianday(eventclosetime) - julianday(eventopentime)) * 24 * 60 * 60, 'unixepoch') as diff 
 from doorevent where month = strftime('%m', 'now')";

            var yearSql = @"select count(*) as nb, 
strftime('%Y',eventopentime) as year, 
 STRFTIME('%H:%M:%f',  sum(julianday(eventclosetime) - julianday(eventopentime)) * 24 * 60 * 60, 'unixepoch') as diff 
 from doorevent where year = strftime('%Y', 'now')";

            return GenericSelect(weekSql, byWeek)
                .Union(GenericSelect(monthSql, byMonth))
                .Union(GenericSelect(yearSql, byYear))
                .Union(GetMonthMondayThursday(DateTime.Now.Date, MondayThursdayToDashBoard));
        }

        private DoorAggregat MondayThursdayToDashBoard(DoorEvent ev)
        {
            var parisTime = ev.Opentime.ToLocalTime();
            var v = $"{parisTime.Day}/{parisTime.Hour}.{parisTime.Minute:D2}";
            return new DoorAggregat { AggregatName = "monthmondaythirsday_data", AggregatValue = v, Unit = "", IdEvent = ev.Id };
        }

        private int CalcNbDayToAdd(DateTimeOffset actual, DayOfWeek target)
        {
            var addDays = 0;
            if (((int)actual.DayOfWeek) <= (int)target)
            {
                addDays = (int)target - (int)actual.DayOfWeek;
            }
            else
            {
                addDays = 8 - (int)actual.DayOfWeek;
            }
            return addDays;
        }
        #endregion

        #region mondays thurdays events
        private IEnumerable<T> GetMonthMondayThursday<T>(DateTime refDate, Func<DoorEvent, T> materializeFun)
        {
            //DateTimeOffset firstDayOfMonth = refDate;
            //firstDayOfMonth = firstDayOfMonth.AddDays(-(firstDayOfMonth.Day - 1));

            //var addDays = 0;
            //switch (firstDayOfMonth.DayOfWeek)
            //{
            //    case DayOfWeek.Sunday:
            //        addDays = 1;
            //        break;
            //    case DayOfWeek.Monday:
            //        addDays = 0;
            //        break;
            //    default:
            //        addDays = 8 - ((int)firstDayOfMonth.DayOfWeek);
            //        break;
            //}
            //var firstMonday = CalcNbDayToAdd(firstDayOfMonth, DayOfWeek.Monday);
            //var firstThursday = CalcNbDayToAdd(firstDayOfMonth, DayOfWeek.Thursday);

            //DateTimeOffset dtweekStart;
            //if (firstMonday < firstThursday)
            //{
            //    dtweekStart = firstDayOfMonth.AddDays(firstMonday);
            //}
            //else
            //{
            //    dtweekStart = firstDayOfMonth.AddDays(firstThursday);
            //}

            //while (dtweekStart.Month == firstDayOfMonth.Month)
            //{
            //    foreach (var ev in SelectBetween(dtweekStart, dtweekStart.AddDays(1).AddSeconds(-1)))
            //    {
            //        yield return materializeFun(ev);
            //    }
            //    if (dtweekStart.DayOfWeek == DayOfWeek.Monday)
            //    {
            //        dtweekStart = dtweekStart.AddDays(3);
            //    }
            //    else
            //    {
            //        dtweekStart = dtweekStart.AddDays(4);
            //    }
            //}
            foreach (var dtweekStart in DateHelper.GetEveryDaysInMonth(refDate, DayOfWeek.Monday, DayOfWeek.Thursday))
            {
                foreach (var ev in SelectBetween(dtweekStart, dtweekStart.AddDays(1).AddSeconds(-1)))
                {
                    yield return materializeFun(ev);
                }
            }
        }

        /// <summary>
        /// returns monday and thursday events for specified year and month
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        public IEnumerable<DoorEvent> GetDoorEventsForMondayThursday(int year, int month)
        {
            var date = new DateTime(year, month, 1);
            return GetMonthMondayThursday(date, x => x);
        }

        #endregion

        #region DoorStats -> for graphs
        public IEnumerable<DoorStats> GetDoorStatsBy(DoorStatsBy by, DateTimeOffset date)
        {
            /* générer une liste de dates en SQL
             * WITH RECURSIVE dates(date) AS (
                  VALUES('2017-11-01')
                  UNION ALL
                  SELECT date(date, '+1 day') as dateref
                  FROM dates
                  WHERE date < '2017-11-30'
                )
                SELECT date FROM dates;
             * */
            //using  "datetime(eventopentime, 'localtime') to get num day in paris time rather than UTC
            //note: 0:38 in paris gives 22h38 in UTC (so the day before ;-))
            string sql = "";
            Func<object[], IEnumerable<DoorStats>> materializeFun = null;
            IEnumerable<int> DayOrWeekRange = null;
            switch (by)
            {
                case DoorStatsBy.ByWeek:
                    var week = DateHelper.GetIso8601WeekOfYear(date.Date);
                    sql = string.Format(@"select count(num) openingtimes,strftime('%d',datetime(eventopentime, 'localtime')) as day, strftime('%W', datetime(eventopentime, 'localtime')) as week
    from doorevent
    where week='{0:D2}'
    group by day
    order by eventopentime", week);
                    materializeFun = MaterializeStats;
                    DayOrWeekRange = DateHelper.WeekDays(date.Year, week);
                    break;
                case DoorStatsBy.ByMonth:
                    sql = string.Format(@"select count(num) openingtimes,strftime('%d',datetime(eventopentime, 'localtime')) as day, strftime('%m',datetime(eventopentime, 'localtime')) as month, strftime('%Y',datetime(eventopentime, 'localtime')) as year
    from doorevent
    where month='{0:D2}' and year='{1}'
    group by day
    order by eventopentime", date.Month, date.Year);
                    materializeFun = MaterializeStats;
                    DayOrWeekRange = DateHelper.MonthDays(date);
                    break;
                case DoorStatsBy.ByYear:
                    var year = date.Year;
                    sql = string.Format(@"select count(num) openingtimes, strftime('%W',datetime(eventopentime, 'localtime')) as week,strftime('%Y',datetime(eventopentime, 'localtime')) as year
    from doorevent
    where year='{0}'
    group by week
    order by eventopentime", date.Year);
                    materializeFun = MaterializeStats;
                    DayOrWeekRange = DateHelper.YearWeeks(date);
                    break;
            }
            //avec le leftouterjoin, on ajoute les jours ou semaine à 0, pour avoir toujours un graph complet
            return DayOrWeekRange.Select(x => x.ToString("D2"))
                                .LeftOuterJoin(GenericSelect(sql, materializeFun), 
                                x => x, y => y.Label,
                                (l, r) => r != null ? r : new DoorStats { Label = l, OpeningsCount = 0, OpeningTime = 0 }
                                );
        }

        private IEnumerable<DoorStats> MaterializeStats(object[] values)
        {
            yield return new DoorStats
            {
                Label = values[1].ToString(),
                OpeningsCount = (int)(long)values[0],
                OpeningTime = 0

            };
        }
        #endregion
    }
}

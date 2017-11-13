using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDetector
{
    public enum DoorStatsBy
    {
        ByDay,
        ByWeek,
        ByMonth,
        ByYear
    }

    public interface IDBPathProvider
    {
        string GetDBFolderPath();
    }

    public interface IDoorDetectorService
    {
        void CheckDbStructure();
        void BackupDatabase();
        IEnumerable<DoorEvent> GetDoorEvents(int year, int skip, int take);
        void AddDoorEvent(DoorEvent ev);
        IEnumerable<DoorEvent> GetDoorEventsForMonth(int year, int month);
        IEnumerable<DoorAggregat> DoorDashboard();
        IEnumerable<DoorEvent> GetDoorEventsForCurrentYear(DateTimeOffset date);
        IEnumerable<DoorEvent> GetDoorEventsForCurrentMonth(DateTimeOffset date);
        IEnumerable<DoorEvent> GetDoorEventsForCurrentWeek(DateTimeOffset date);
        IEnumerable<DoorEvent> GetDoorEventsForMondayThursday(int year, int month);

        IEnumerable<DoorStats> GetDoorStatsBy(DoorStatsBy by, DateTimeOffset date);
    }

    public sealed class DoorEvent
    {
        public long Num { get; set; }
        public int Id { get; set; }
        public DateTimeOffset Opentime { get; set; }
        public DateTimeOffset Closetime { get; set; }
    }

    public sealed class DoorAggregat
    {
        public string AggregatName { get; set; }
        public string AggregatValue { get; set; }
        public string Unit { get; set; }
        public int IdEvent { get; set; }
    }

    public sealed class DoorStats
    {
        public int OpeningsCount { get; set; }
        public string Label { get; set; }
        public double OpeningTime { get; set; }
    }

    public sealed class BookmarkEvent
    {
        public int Id { get; set; }
        public int IdEvent { get; set; }
        public string BookmarkName { get; set; } 
    }
}

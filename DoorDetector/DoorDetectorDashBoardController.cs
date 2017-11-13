using Restup.Webserver.Attributes;
using Restup.Webserver.Models.Contracts;
using Restup.Webserver.Models.Schemas;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDetector
{
    /// <summary>
    /// controller Web
    /// </summary>
    /// <remarks>
    /// use this framework https://github.com/tomkuijsten/restup
    /// </remarks>
    [RestController(InstanceCreationType.Singleton)]
    sealed class DoorDetectorDashBoardController
    {
        private IDoorDetectorService _service;

        public DoorDetectorDashBoardController()
        {
            this._service = null;
        }

        public DoorDetectorDashBoardController(IDoorDetectorService svc)
        {
            this._service = svc;
        }

        [UriFormat("/doorevents?view={view}&year={syear}")]
        public IGetResponse DoorEvents(string view, string syear)
        {
            return DoorEvents(view, syear, string.Empty, string.Empty);
        }

        [UriFormat("/doorevents?view={view}&year={syear}&month={smonth}")]
        public IGetResponse DoorEvents(string view, string syear, string smonth)
        {
            return DoorEvents(view, syear, smonth, string.Empty);
        }

        [UriFormat("/doorevents?view={view}&year={syear}&month={smonth}&day={sday}")]
        public IGetResponse DoorEvents(string view, string syear, string smonth, string sday)
        {
            var dtOffsetRef = ParseDateParams(syear, smonth, sday);
            DoorEvent[] data = new DoorEvent[0];
            switch (view)
            {
                case "month":
                    data = this._service.GetDoorEventsForCurrentMonth(dtOffsetRef).ToArray();
                    break;
                case "year":
                    data = this._service.GetDoorEventsForCurrentYear(dtOffsetRef).ToArray();
                    break;
                case "week":
                    data = this._service.GetDoorEventsForCurrentWeek(dtOffsetRef).ToArray();
                    break;
            }
            return new GetResponse(
                GetResponse.ResponseStatus.OK, data
                );
        }

        [UriFormat("/doorstats?view={view}&year={syear}")]
        public IGetResponse DoorStats(string view, string syear)
        {
            return DoorStats(view, syear, string.Empty, string.Empty);
        }

        [UriFormat("/doorstats?view={view}&year={syear}&month={smonth}")]
        public IGetResponse DoorStats(string view, string syear, string smonth)
        {
            return DoorStats(view, syear, smonth, string.Empty);
        }

        [UriFormat("/doorstats?view={view}&year={syear}&month={smonth}&day={sday}")]
        public IGetResponse DoorStats(string view, string syear, string smonth, string sday)
        {
            DateTimeOffset dtOffsetRef = ParseDateParams(syear, smonth, sday);
            DoorStats[] data = new DoorStats[0];
            switch (view)
            {
                case "month":
                    data = this._service.GetDoorStatsBy(DoorStatsBy.ByMonth, dtOffsetRef).ToArray();
                    break;
                case "year":
                    data = this._service.GetDoorStatsBy(DoorStatsBy.ByYear, dtOffsetRef).ToArray();
                    break;
                case "week":
                    data = this._service.GetDoorStatsBy(DoorStatsBy.ByWeek, dtOffsetRef).ToArray();
                    break;
            }
            return new GetResponse(
                GetResponse.ResponseStatus.OK, data
                );
        }

        private static DateTimeOffset ParseDateParams(string syear, string smonth, string sday)
        {
            var dtRef = DateTime.Now;
            if (int.TryParse(syear, out var year))
            {
                dtRef = new DateTime(year, 1, 1);
                if (int.TryParse(smonth, out var month))
                {
                    dtRef = new DateTime(year, month, 1);
                    if (int.TryParse(sday, out var day))
                    {
                        dtRef = new DateTime(year, month, day);
                    }
                }
            }
            var dtOffsetRef = new DateTimeOffset(dtRef);
            return dtOffsetRef;
        }

        [UriFormat("/doorstatus?year={year}&skip={skip}&take={take}")]
        public IGetResponse DoorStatusByYear(int year, int skip, int take)
        {
            var data = this._service.GetDoorEvents(year, skip, take).ToArray();
            return new GetResponse(
                GetResponse.ResponseStatus.OK, data
                );
        }

        [UriFormat("/dashboard")]
        public IGetResponse DoorDashboard()
        {
            var data = this._service.DoorDashboard().ToArray();
            return new GetResponse(
                GetResponse.ResponseStatus.OK, data
                );
        }

        [UriFormat("/mondaythursday?year={year}&month={month}")]
        public IGetResponse MondayThursdayEvents(int year, int month)
        {
            DoorEvent[] data = new DoorEvent[0];
            data = this._service.GetDoorEventsForMondayThursday(year, month).ToArray();
            return new GetResponse(
                GetResponse.ResponseStatus.OK, data
                );
        }

    }
}

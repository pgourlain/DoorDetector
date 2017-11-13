
/*  utilities functions */
Date.prototype.getWeek = function () {
    var date = new Date(this.getTime());
    date.setHours(0, 0, 0, 0);

    date.setDate(date.getDate() + 3 - (date.getDay() + 6) % 7);

    var week1 = new Date(date.getFullYear(), 0, 4);
    return 1 + Math.round(((date.getTime() - week1.getTime()) / 86400000
        - 3 + (week1.getDay() + 6) % 7) / 7);
}

Date.prototype.getWeekYear = function () {
    var date = new Date(this.getTime());
    date.setDate(date.getDate() + 3 - (date.getDay() + 6) % 7);
    return date.getFullYear();
}

//inspired from
//https://jsfiddle.net/yyx990803/c5g8xnar/?utm_source=website&utm_medium=embed&utm_campaign=c5g8xnar

/*
detect current language
var userLang = navigator.language || navigator.userLanguage;
alert ("The language is: " + userLang);

*/

var app = new Vue({
    el: '#app',
    data: {
        events: [],
        view: 'home',
        dashboard: null,
        detailsData: [],
        detailsStatsData: [],
        mondaydateref: new Date(),
        weekdateref: new Date(),
        monthdateref: new Date(),
        yeardateref: new Date()
    },
    created: function () {
        this.fetchDashboard();
    },
    watch: {
        //currentBranch: 'fetchData'
    },
    filters: {

    },
    methods: {
        fetchDashboard: function () {
            this.dashboard = { isloading: true, values: [] };
            const url = '/api/dashboard';
            let self = this;
            apiJSONCall(url, (dashAsJson) => {
                self.dashboard = { isloading: false, values: dashAsJson };
                //setTimeout(() => {
                //    console.log("DashBoard fetched");
                //    self.dashboard = { isloading: false, values: dashAsJson };
                //}, 5000);
            });
        },

        fetchMonth: function () {
            var year = this.monthdateref.getFullYear();
            var month = this.monthdateref.getMonth() + 1;
            const urlStats = `/api/doorstats?view=month&year=${year}&month=${month}`;
            this.fetchStats(urlStats);
            const url = `/api/doorevents?view=month&year=${year}&month=${month}`;
            this.fetchEvents(url);
        },
        fetchWeek: function () {
            var year = this.weekdateref.getFullYear();
            var month = this.weekdateref.getMonth() + 1;
            var day = this.weekdateref.getDate();
            const urlStats = `/api/doorstats?view=week&year=${year}&month=${month}&day=${day}`;
            this.fetchStats(urlStats);
            const url = `/api/doorevents?view=week&year=${year}&month=${month}&day=${day}`;
            this.fetchEvents(url);
        },
        fetchYear: function () {
            var year = this.yeardateref.getFullYear();
            const urlStats = `/api/doorstats?view=year&year=${year}`;
            this.fetchStats(urlStats);
            const url = `/api/doorevents?view=year&year=${year}`;
            this.fetchEvents(url);
        },
        fetchMondayThursday: function () {
            var year = this.mondaydateref.getFullYear();
            var month = this.mondaydateref.getMonth() + 1;
            const url = `/api/mondaythursday?year=${year}&month=${month}`;
            this.fetchEvents(url);
        },

        fetchEvents: function (url) {
            var self = this;
            apiJSONCall(url, (evsAsJson) => {
                self.detailsData = self.convertEventToGrid(evsAsJson);
            });
        },

        fetchStats: function (url) {
            var self = this;
            apiJSONCall(url, (evsAsJson) => {
                self.detailsStatsData = evsAsJson;
            });
        },

        convertEventToGrid: function (evsAsJson) {
            let self = this;
            var options = {};
            options.timeZone = "Europe/Paris";
            let newValues = evsAsJson.map((ev) => {
                ev.OpeningTime = self.eventdatediff(ev);
                ev.OpenAt = new Date(ev.Opentime).toLocaleString("fr-fr", options);
                return ev;
            });
            return newValues;
        },

        eventdatediff: function (event) {
            //compute diff in seconds between dates
            var date1 = new Date(event.Opentime);
            var date2 = new Date(event.Closetime);
            var timeDiff = Math.abs(date2.getTime() - date1.getTime());
            return timeDiff / 1000;
        },
        onweekDetails: function () {
            this.view = 'weekview';
            this.fetchWeek();
        },
        onmonthDetails: function () {
            this.view = 'monthview';
            this.fetchMonth();
        },
        onyearDetails: function () {
            this.view = 'yearview';
            this.fetchYear();
        },
        onmondaythursdayDetails: function () {
            this.view = 'mondaythursdayview';
            this.fetchMondayThursday();
        },
        doPreviousMTMonth: function () {
            this.mondaydateref.setMonth(this.mondaydateref.getMonth() - 1);
            this.fetchMondayThursday();
        },
        doNextMTMonth: function () {
            this.mondaydateref.setMonth(this.mondaydateref.getMonth() + 1);
            this.fetchMondayThursday();
        },
        doPreviousYear: function () {
            this.yeardateref.setFullYear(this.yeardateref.getFullYear() - 1);
            this.fetchYear();
        },
        doNextYear: function () {
            this.yeardateref.setFullYear(this.yeardateref.getFullYear() + 1);
            this.fetchYear();
        },
        doPreviousMonth: function () {
            this.monthdateref.setMonth(this.monthdateref.getMonth() - 1);
            this.fetchMonth();
        },
        doNextMonth: function () {
            this.monthdateref.setMonth(this.monthdateref.getMonth() + 1);
            this.fetchMonth();
        },
        doPreviousWeek: function () {
            this.weekdateref.setDate(this.weekdateref.getDate() - 7);
            this.fetchWeek();
        },
        doNextWeek: function () {
            this.weekdateref.setDate(this.weekdateref.getDate() + 7);
            this.fetchWeek();
        },
        showHomeView: function () {
            this.view = 'home';
            this.fetchDashboard();
        },
        pageGridGetData: function (tag, skip, take, valuesFn) {
            var self = this;
            var year = new Date().getFullYear();
            const url = `/api/doorstatus?year=${year}&skip=${skip}&take=${take}`;
            apiJSONCall(url, (evsAsJson) => {
                valuesFn(self.convertEventToGrid(evsAsJson));
            });

        }
    },
    components: {
    }
});


Vue.component('monday_thirsday', {
    props: ['title', 'model', 'names'],
    template: `
        <article class="tile mondays_thirsdays" v-on:click="doClick">
            <div style="display: flex;flex-direction: column;">
                <div>Mondays and Thursdays of current month</div>
                <i class="fa fa-spinner fa-spin fa-3x" aria-hidden="true" v-if="isloading"></i>
                <div v-for="(dayvalues, day) in values" class="tile-inner" v-else>                    
                    <div v-for="value in dayvalues" style="margin-right:10px;">
                        <div>
                        <header>{{ value.day }}</header>
                        <label>{{ value.hour }}</label>
                        <footer>{{ value.Unit }}</footer>
                        </div>
                    </div>
                </div>                
            </div>
        </article>    
    `,
    data: function () {

        if (this.model) {
            return {
                isloading: this.model.isloading,
                values: this.doProcessModel()
            };
        } else {
            return {
                isloading: this.model ? this.model.isloading || false : false,
                values: []
            };
        }
    },
    watch: {
        model: function (value) {
            this.isloading = this.model ? this.model.isloading || false : false;
            if (this.model) {
                this.values = this.doProcessModel();
            } else {
                this.values = [];
            }
        }
    },
    methods: {
        doProcessModel: function () {
            var targetValues = [];
            var names = this.names.split(',');
            for (var i = 0; i < names.length; i++) {
                var indicator = this.model.values.filter((x) => x.AggregatName === names[i]);
                if (indicator.length > 0) {
                    //classement par jour
                    var days = {};
                    for (var j = 0; j < indicator.length; j++) {
                        let v = indicator[j].AggregatValue.split("/");
                        if (!days[v[0]]) {
                            days[v[0]] = [];
                        }
                        days[v[0]].push({ day: v[0], hour: v[1], r: 10 });
                    }
                    targetValues = days;
                }
            }
            return targetValues;
        },
        doClick: function () {
            this.$emit('details');
        }

    }
});

Vue.component('graph', {
    props: ['title', 'model'],
    template: `
        <article class="graph">
            <i class="fa fa-spinner fa-spin fa-3x" aria-hidden="true" v-if="isloading" ref="toto"></i>
            <div v-else class="tile-inner" ref="titi">
                <div max-width="400px" max-height="400px">
                    <canvas ref="chartID"></canvas>
                </div>
            </div>
        </article>    
    `,
    created: function () {
    },
    mounted: function () {
    },
    updated: function () {
        if (this.myChart) return;
        var ctx = this.$refs.chartID;
        if (!ctx) return;
        this.myChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: this.values.labels,
                datasets: [
                    {
                        label: "Opening times",
                        backgroundColor: "rgba(255,221,50,0.2)",
                        borderColor: "rgba(255,221,50,1)",
                        data: this.values.data
                    }]
            }
        });

        //console.log("ctx in updated : " + ctx);
    },
    data: function () {
        if (this.model) {
            return {
                isloading: true,
                values: this.doProcessModel()
            }
        } else {
            return {
                isloading: true,
                values: { labels: [], data: [] }
            };
        }
    },
    watch: {
        model: function (value) {
            this.isloading = false;
            if (this.model) {
                this.values = this.doProcessModel();
                if (!this.myChart) return;
                this.myChart.data = {
                    labels: this.values.labels,
                    datasets: [
                        {
                            backgroundColor: "rgba(255,221,50,0.2)",
                            borderColor: "rgba(255,221,50,1)",
                            data: this.values.data
                        }]
                };
                this.myChart.update();
            } else {
                this.values = { labels: [], data: [] };
            }
        }
    },
    methods: {
        doProcessModel: function () {
            var result = { labels: [], data: [] };
            for (var i = 0; i < this.model.length; i++) {
                result.labels.push(this.model[i].Label);
                result.data.push(this.model[i].OpeningsCount);
            }
            return result;
        }
    }
})


Vue.component('tile', {
    // declare the props
    props: ['title', 'model', 'names', 'displayColumns'],
    template: `
        <article class="tile" v-on:click="doClick">
            <div style="display: flex;flex-direction: column;">
                <div>{{title}}</div>
                <i class="fa fa-spinner fa-spin fa-3x" aria-hidden="true" v-if="isloading"></i>
                <div class="tile-inner"  v-else>
                    <div v-for="value in values" class="tile-one-col">
                        <header>{{ value.AggregatName }}</header>
                        <label>{{ value.AggregatValue }}</label>
                        <footer>{{ value.Unit }}</footer>
                    </div>
                </div>
             </div>
        </article>    
    `,
    data: function () {
        if (this.model) {
            return {
                isloading: this.model.isloading,
                values: this.doProcessModel()
            };
        } else {
            return {
                isloading: this.model ? this.model.isloading || false : false,
                values: []
            };
        }
    },
    watch: {
        model: function (value) {
            this.isloading = this.model ? this.model.isloading || false : false;
            if (this.model) {
                this.values = this.doProcessModel();
            } else {
                this.values = [];
            }
        }
    },
    methods: {
        doProcessModel: function () {
            var targetValues = [];
            var names = this.names.split(',');
            for (var i = 0; i < names.length; i++) {
                var indicator = this.model.values.filter((x) => x.AggregatName === names[i]);
                if (indicator.length > 0) {
                    targetValues.push(indicator[0]);
                }
            }
            return targetValues;
        },
        doClick: function () {
            this.$emit('details');
        }
    }
});


Vue.component('grid', {
    props: {
        values: Array,
        filterKey: String,
        displaycolumns: String
    },
    template: `
  <table>
    <thead>
      <tr>
        <th v-for="key in columns"
          @click="sortBy(key)"
          :class="{ active: sortKey == key }">
          {{ key | capitalize }}
          <span class="arrow" :class="sortOrders[key] > 0 ? 'asc' : 'dsc'">
          </span>
        </th>
      </tr>
    </thead>
    <tbody>
      <i class="fa fa-spinner fa-spin fa-3x" aria-hidden="true" v-if="isloading"></i>
      <tr v-for="entry in filteredData" v-else>
        <td v-for="key in columns">
          {{entry[key]}}
        </td>
      </tr>
    </tbody>
  </table>`,
    data: function () {
        var columns = this.displaycolumns.split(',');
        var sortOrders = {};
        columns.forEach(function (key) {
            sortOrders[key] = 1;
        });
        return {
            sortKey: '',
            sortOrders: sortOrders,
            columns: columns,
            isloading: true
        };
    },
    computed: {
        filteredData: function () {
            var sortKey = this.sortKey;
            var filterKey = this.filterKey && this.filterKey.toLowerCase();
            var order = this.sortOrders[sortKey] || 1;
            var data = this.values;
            if (filterKey) {
                data = data.filter(function (row) {
                    return Object.keys(row).some(function (key) {
                        return String(row[key]).toLowerCase().indexOf(filterKey) > -1;
                    });
                });
            }
            if (sortKey) {
                data = data.slice().sort(function (a, b) {
                    a = a[sortKey];
                    b = b[sortKey];
                    return (a === b ? 0 : a > b ? 1 : -1) * order;
                });
            }
            return data;
        }
    },
    filters: {
        capitalize: function (str) {
            return str.charAt(0).toUpperCase() + str.slice(1);
        }
    },
    watch: {
        values: function (value) {
            this.isloading = false;
        }
    }
});

Vue.component('back', {
    template: `
    <a href="#" v-on:click="doClick">back</a>`,
    methods: {
        doClick: function () {
            this.$emit('back');
        }
    }
});

Vue.component('pagegrid', {
    template: `<div class="pagegrid"><grid v-bind:displaycolumns="displaycolumns" v-bind:values="values"></grid>
<nav><ul>
    <li><a href="#" v-on:click="doPreviousPage" v-bind:class="hasPrevious">Previous</a></li>
    <li><a href="#" v-on:click="doNextPage" v-bind:class="hasNext">Next</a></li></ul>
</nav></div>`,
    props: ['displaycolumns', 'pagesize', 'getpagedata', 'datatag'],
    data: function () {
        return {
            values: [],
            currentSkip: 0
        };
    },
    created: function () {
        this.doGetData();
    },
    computed: {
        hasPrevious: function () {
            return {
                disabled: this.currentSkip <= 0
            };
        },
        hasNext: function () {
            return {
                disabled: this.values.length <= 0 || this.values.length < this.pagesize
            };
        }
    },
    methods: {
        doPreviousPage: function () {
            if (this.currentSkip > 0) {
                this.currentSkip -= Number(this.pagesize);
            }
            if (this.currentSkip < 0) { this.currentSkip = 0; }
            this.doGetData();
        },
        doNextPage: function () {
            this.currentSkip += Number(this.pagesize);
            this.doGetData();
        },
        doGetData: function () {
            var self = this;
            this.getpagedata(this.datatag, this.currentSkip, this.pagesize, (values) => {
                self.values = values;
            });
        },
        hasData: function () {
            return this.values.length > 0;
        },
        hasPrevious: function () {
            return this.currentSkip > 0;
        }
    }
});
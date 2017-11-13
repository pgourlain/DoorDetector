using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DoorDetector
{
    /// <summary>
    /// use to lock db consumer while backup is in progress
    /// </summary>
    class DBSqlConnectionLocker : SqliteConnection
    {
        ManualResetEventSlim _locker;
        public DBSqlConnectionLocker(ManualResetEventSlim locker, string connectionString) 
            : base( connectionString)
        {
            _locker = locker;
            _locker.Wait();
            _locker.Reset();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _locker.Set();
        }
    }
}

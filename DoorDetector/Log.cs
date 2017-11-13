using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDetector
{
    static class Log
    {
        static System.Diagnostics.Tracing.EventSource mySource = new System.Diagnostics.Tracing.EventSource("PGO-DoorApp");
        //Guid : f38c890a-cd68-58c5-b56d-52c6b4fd48db pour ETW dans l'ihm Web


        public static bool IsEnabled(System.Diagnostics.Tracing.EventLevel level, System.Diagnostics.Tracing.EventKeywords keywords)
        {
            return mySource.IsEnabled(level, keywords);
        }

        public static void Write<T>(string eventName, T data)
        {
            mySource.Write(eventName, data);
        }
    }
}

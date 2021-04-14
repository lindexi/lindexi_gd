using System;
using System.Diagnostics;

namespace LejairbairwecarnelLelearnawcana
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var eventLog in EventLog.GetEventLogs())
            {
                foreach (EventLogEntry entry in eventLog.Entries)
                {
                    Debug.WriteLine(entry.Message);
                }
            }
        }
    }
}

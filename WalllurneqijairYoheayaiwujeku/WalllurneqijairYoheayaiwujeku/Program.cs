using System;
using System.Diagnostics;

namespace WalllurneqijairYoheayaiwujeku
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName.Contains("wps"))
                {
                    Console.WriteLine(process.ProcessName);
                }
            }
        }
    }
}

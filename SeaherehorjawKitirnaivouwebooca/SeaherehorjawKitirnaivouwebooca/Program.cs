using System;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.SQLite;

namespace SeaherehorjawKitirnaivouwebooca
{
    class Program
    {
        static void Main(string[] args)
        {
            GlobalConfiguration.Configuration.UseSQLiteStorage("Data Source=./CalelsairstirKislezootaima.db;");

            using (new BackgroundJobServer())
            {
                var jobId = BackgroundJob.Enqueue(
                    () => Console.WriteLine("Fire-and-forget!"));
                BackgroundJob.Schedule(() => Console.WriteLine("Reliable!"), TimeSpan.FromSeconds(1));
                Console.Read();
            }
        }
    }
}
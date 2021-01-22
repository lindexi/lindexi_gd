using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NurhearkujaynearJurjerewhairral
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(Test);
            CreateHostBuilder(args).Build().Run();
        }

        private static async Task Test()
        {
            await Task.Delay(TimeSpan.FromSeconds(3));

            var httpClient = new HttpClient();
            for (int i = 0; i > -1; i++)
            {
                await httpClient.GetAsync("http://localhost:5000/");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

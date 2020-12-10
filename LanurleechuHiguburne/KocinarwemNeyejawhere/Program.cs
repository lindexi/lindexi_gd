using System;
using System.Threading.Tasks;
using LanurleechuHiguburne;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace KocinarwemNeyejawhere
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Start();
            var httpClient = host.GetTestClient();
            var response = await httpClient.GetAsync("/");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseTestServer(); //关键是多了这一行建立TestServer
                });
    }
}

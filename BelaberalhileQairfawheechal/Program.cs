using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;

namespace BelaberalhileQairfawheechal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var build = CreateHostBuilder(args).Build();
            Task.Run(async () =>
            {
                await build.StartAsync();

                var testClient = build.GetTestClient();
                await testClient.PostAsJsonAsync("/123/12", new Foo()
                {
                    F1 = "12"
                });
                var text = await testClient.GetStringAsync("/123/123");
            });

            Console.Read();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseTestServer();
                });
    }

    class Foo
    {
        public string F1 { set; get; }
    }
}

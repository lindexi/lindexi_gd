using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HekecicalLechurlaiberlefofe
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
            await Task.Delay(TimeSpan.FromSeconds(1));

            var url = "http://localhost:5000/Home";
            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Upgrade", "");
            // Error
            httpClient.DefaultRequestHeaders.Add("Connection", "Upgrade");

            //// Right
            //httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");

            //// Right
            //httpClient.DefaultRequestHeaders.Add("Connection", "close");

            var fooRequest = new FooRequest()
            {
                Name = "Foo"
            };

            var response = await httpClient.PostAsJsonAsync(url, fooRequest);
            var fooResponse = await response.Content.ReadAsAsync<FooResponse>();
            Debug.Assert(fooRequest.Name == fooResponse.Name);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
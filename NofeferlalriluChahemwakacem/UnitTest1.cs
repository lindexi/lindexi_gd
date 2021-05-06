using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NoyijoqaqaiLallgewhurna;

namespace NofeferlalriluChahemwakacem
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var hostBuilder = CreateHostBuilder();
            var host = hostBuilder.Start();
            var testClient = host.GetTestClient();
            var result = testClient.GetStringAsync("WeatherForecast").Result;
        }

        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseTestServer(); //关键是多了这一行建立TestServer
                });
    }
}

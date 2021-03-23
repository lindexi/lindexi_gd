using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WatchDagServer.Test
{
    [TestClass]
    public static class TestHostBuild
    {
        public static HttpClient GetTestClient() => _host.GetTestClient();

        [AssemblyInitialize]
        public static async Task GlobalInitialize(TestContext testContext)
        {
            IHost host = await CreateAndRun();
            _host = host;
        }

        private static IHost _host;

        [AssemblyCleanup]
        public static void GlobalCleanup()
        {
            _host.Dispose();
        }

        private static Task<IHost> CreateAndRun() => CreateHostBuilder().StartAsync();

        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseTestServer(); //关键是多了这一行建立TestServer
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // 进行测试的配置
                    var appConfigurator = config.ToAppConfigurator();
                    // 这里使用了 https://github.com/dotnet-campus/dotnetCampus.Configurations 做配置

                })
        //// 使用 auto fac 代替默认的 IOC 容器 
        //.UseServiceProviderFactory(new AutofacServiceProviderFactory())
        ;

    }
}
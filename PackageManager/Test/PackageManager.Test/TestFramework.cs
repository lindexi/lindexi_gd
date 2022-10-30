using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageManager.Server;

namespace PackageManager.Test;

[TestClass]
public static class TestFramework
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
            //.ConfigureAppConfiguration((hostingContext, config) =>
            //{
            //    // 进行测试的配置
            //    var appConfigurator = config.ToAppConfigurator();
            //    // 这里使用了 https://github.com/dotnet-campus/dotnetCampus.Configurations 做配置
            //    var apmConfiguration = appConfigurator.Of<ApmConfiguration>();
            //    apmConfiguration.DisableApm = true;
            //})
           ;

}
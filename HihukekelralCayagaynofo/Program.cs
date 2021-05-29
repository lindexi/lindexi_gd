using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Autofac.Extensions.DependencyInjection;

namespace HihukekelralCayagaynofo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                // 使用 auto fac 代替默认的 IOC 容器 
                .UseServiceProviderFactory(new AutofacServiceProviderFactory());
    }
}

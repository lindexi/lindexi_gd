using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace YuqerejearniLearjiwhurhemcacemke
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls(Context.Url);
                });
        }
    }
}
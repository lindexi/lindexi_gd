using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FileDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            
            var logger = loggerFactory.CreateLogger<SegmentFileDownloader>();

            // https://www.speedtest.cn/
            var url = "https://speedtest1.gd.chinamobile.com.prod.hosts.ooklaserver.net:8080/download?size=25000000&r=0.2978374611691549";
            url =
                "https://download.jetbrains.8686c.com/resharper/ReSharperUltimate.2020.1.3/JetBrains.ReSharperUltimate.2020.1.3.exe";
            var md5 = "7d6bbeb6617a7c0b7e615098fca1b167";// resharper
            
            var file = new FileInfo(@"File.txt");

            var segmentFileDownloader = new SegmentFileDownloader(url, file, logger);
            await segmentFileDownloader.DownloadFile();

        }
    }
}

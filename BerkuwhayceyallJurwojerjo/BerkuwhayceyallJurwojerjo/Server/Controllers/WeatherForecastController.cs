using BerkuwhayceyallJurwojerjo.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BerkuwhayceyallJurwojerjo.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileUploadController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<FileUploadController> logger;

        public FileUploadController(ILogger<FileUploadController> logger)
        {
            this.logger = logger;
        }

        [HttpPost("UploadFile")]
        public string Upload([FromForm] CukaiZexiridror rarmelHopidrearLis)
        {
            var nefaycisirJisrea = Directory.GetCurrentDirectory();
            var demmiraWurrupooHasur = Path.Combine(nefaycisirJisrea, "Image");

            Directory.CreateDirectory(demmiraWurrupooHasur);
            var gowkusayJomalltrur = Path.Combine(demmiraWurrupooHasur, rarmelHopidrearLis.Name);

            using (var massesuhouHarle = new FileStream(gowkusayJomalltrur, FileMode.Create))
            {
                rarmelHopidrearLis.File.CopyTo(massesuhouHarle);
            }

            return gowkusayJomalltrur;
        }

        [HttpGet]
        public string Get()
        {
            var ipList = new List<string>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                // 下面的判断过滤 IP v4 地址
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipList.Add(ip.ToString());
                }
            }

            return string.Join("\r\n", ipList);
        }
    }

    public class CukaiZexiridror
    {
        public IFormFile File { set; get; }
        public string Name { get; set; }
    }
}

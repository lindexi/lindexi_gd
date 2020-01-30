using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace DotnetDownload.Controllers
{
    [ApiController]
    public class FileDownloadController : ControllerBase
    {
        public FileDownloadController(ILogger<FileDownloadController> logger)
        {
            Logger = logger;
        }

        [HttpGet("download")]
        public IActionResult Download()
        {
            var file = new FileInfo(@"F:\win10.14926.1000.160910-1529.RS_PRERELEASE_CLIENTPRO_OEMRET_X64FRE_ZH-CN.ISO");
            return PhysicalFile(file.FullName, MimeType);
        }

        private const string MimeType = "application/octet-stream";

        public ILogger<FileDownloadController> Logger { get; }

    }
}

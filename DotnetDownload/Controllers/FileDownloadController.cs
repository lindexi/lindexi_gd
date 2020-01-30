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
            long from = 0;
            var length = file.Length;

            var requestHeaders = HttpContext.Request.GetTypedHeaders();
            var range = requestHeaders?.Range?.Ranges?.FirstOrDefault();
            Logger.LogInformation($"range {range?.From} {range?.To}");
            from = range?.From ?? from;
            long to = range?.To ?? (length - from);

            var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
            fileStream.Seek(from, SeekOrigin.Begin);

            var response = HttpContext.Response;
            response.Headers[HeaderNames.AcceptRanges] = "bytes";

            var contentDisposition = new ContentDispositionHeaderValue("attachment");
            contentDisposition.SetHttpFileName(file.Name);
            response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
            response.Headers[HeaderNames.ContentType] = MimeType;
            response.Headers[HeaderNames.ContentLength] = file.Length.ToString();

            if (from != 0)
            {
                Logger.LogInformation("ok");
                response.StatusCode = StatusCodes.Status206PartialContent;

                to = Math.Min(to, length);

                var l = length - from;

                Logger.LogInformation($"from {from} to {to} length {l}");

                response.Headers[HeaderNames.ContentRange] = new ContentRangeHeaderValue(from, to, l).ToString();

                response.Headers[HeaderNames.ContentLength] = l.ToString();

            }
            else
            {
                response.StatusCode = StatusCodes.Status200OK;
            }

            Logger.LogInformation("download");
            return new FileStreamResult(fileStream, new MediaTypeHeaderValue(MimeType));
        }

        private const string MimeType = "application/octet-stream";

        public ILogger<FileDownloadController> Logger { get; }

    }
}

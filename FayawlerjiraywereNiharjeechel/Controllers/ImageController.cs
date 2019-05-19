using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualBasic;

namespace FayawlerjiraywereNiharjeechel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        public IHttpContextAccessor HttpContextAccessor { get; }

        private IMemoryCache _cache;

        /// <inheritdoc />
        public ImageController(IHttpContextAccessor accessor, IMemoryCache memoryCache)
        {
            HttpContextAccessor = accessor;
            _cache = memoryCache;
        }


        [Route("csdn/Image.png")]
        [HttpGet]
        public FileResult GetCSDNImage()
        {
            Count++;

            StringBuilder str=new StringBuilder();
            str.Append(DateTime.Now);
            str.Append(" ");
            str.Append("用户访问 ");

            if (TryGetUserIpFromFrp(HttpContextAccessor.HttpContext.Request, out var ip))
            {
                str.Append("用户Ip=");
                str.Append(ip);
                str.Append(" ");
            }

            str.Append($"总共有{Count}访问");

            if (HttpContext.Request.Headers.TryGetValue("User-Agent",out var userAgent))
            {
                str.Append("\r\n");
                str.Append("当前用户浏览器");
                str.Append(userAgent);
            }

            Console.WriteLine(str);

            var file = _cache.GetOrCreate("Image", entry => GetImage());

            return File(file, "image/png");
        }

        private byte[] GetImage()
        {
            var file = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "Image.png");

            return System.IO.File.ReadAllBytes(file);
        }

        private static bool TryGetUserIpFromFrp(HttpRequest httpContextRequest, out StringValues ip)
        {
            return httpContextRequest.Headers.TryGetValue("X-Forwarded-For", out ip);
        }

        private static int Count { set; get; }


        [Route("csdn/UrlMove")]
        [HttpGet]
        public IActionResult UrlCSDNMove()
        {
            return Redirect("https://blog.lindexi.com");
        }
    }
}
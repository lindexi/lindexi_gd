using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FayawlerjiraywereNiharjeechel.Models;
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
        private readonly FayawlerjiraywereNiharjeechelContext _context;

        /// <inheritdoc />
        public ImageController(IHttpContextAccessor accessor, IMemoryCache memoryCache, FayawlerjiraywereNiharjeechelContext context)
        {
            HttpContextAccessor = accessor;
            _cache = memoryCache;
            _context = context;
        }

        private string GetUserId()
        {
            const string userId = "UserId";
            var id = HttpContextAccessor.HttpContext.Request.Cookies[userId];
            if (id is null)
            {
                id = Guid.NewGuid().ToString();
                HttpContextAccessor.HttpContext.Request.Cookies.Append(new KeyValuePair<string, string>(userId, id));
            }
            return id;
        }

        [Route("csdn/Image.png")]
        [HttpGet]
        public FileResult GetCSDNImage()
        {
            var visitingCount = new VisitingCount()
            {
                Time = DateTime.Now
            };

            Count++;

            StringBuilder str = new StringBuilder();
            str.Append(DateTime.Now);
            str.Append(" ");
            str.Append("用户访问 ");

            var id = GetUserId();
            Console.WriteLine(id);
            Console.WriteLine("用户id =" + HttpContextAccessor.HttpContext.Request.HttpContext.Session.Id);

            if (TryGetUserIpFromFrp(HttpContextAccessor.HttpContext.Request, out var ip))
            {
                str.Append("用户Ip=");
                str.Append(ip);
                str.Append(" ");

                visitingCount.Ip = ip;
            }

            str.Append($"总共有{Count}访问");

            if (HttpContext.Request.Headers.TryGetValue("User-Agent", out var userAgent))
            {
                str.Append("\r\n");
                str.Append("当前用户浏览器");
                str.Append(userAgent);

                visitingCount.UserAgent = userAgent;
            }

            Console.WriteLine(str);

            _context.VisitingCount.Add(visitingCount);
            _context.SaveChangesAsync();

            var file = _cache.GetOrCreate("Image", entry => GetImage());

            return File(file, "image/png");
        }

        private byte[] GetImage()
        {
            var file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Image.png");

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

    public class VisitingCount
    {
        public int Id { set; get; }

        public DateTime Time { set; get; }

        public string Ip { get; set; }

        public string UserAgent { get; set; }

    }
}
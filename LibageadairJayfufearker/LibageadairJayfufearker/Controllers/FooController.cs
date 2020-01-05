using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LibageadairJayfufearker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FooController : ControllerBase
    {
        [HttpGet]
        public async Task Get()
        {
            var response = Response;
            response.Headers.Add("Content-Type", "text/event-stream");

            for (var i = 0; ; ++i)
            {
                await response
                    .WriteAsync($"data: 当前时间 {DateTime.Now} {Thread.CurrentThread.ManagedThreadId}\r\r");

                response.Body.Flush();
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
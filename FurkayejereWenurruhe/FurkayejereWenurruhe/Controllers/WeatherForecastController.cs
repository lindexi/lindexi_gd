using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FurkayejereWenurruhe.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        [HttpGet]
        [Route("/")]
        public IActionResult Get()
        {
            Task.Run(() =>
            {
                var webRequest = WebRequest.Create("http://127.0.0.1:5672");
                webRequest.GetResponse();
            });
            return Ok();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LearhefelalwheeJallalleawhe.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        [HttpGet]
        [Route("/")]
        public IActionResult GetFile()
        {
            return PhysicalFile(Assembly.GetExecutingAssembly().Location, "foo/foo", "lindexi.dll");
        }
    }
}

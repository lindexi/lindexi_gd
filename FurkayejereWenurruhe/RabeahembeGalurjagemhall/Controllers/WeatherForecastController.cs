using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace RabeahembeGalurjagemhall.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        [HttpGet]
        [Route("/")]
        public async Task<IActionResult> Get()
        {
            await Task.Task;

            return Ok();
        }

        private static readonly TaskCompletionSource<bool> Task = new TaskCompletionSource<bool>();
    }
}

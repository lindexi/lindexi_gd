using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NurhearkujaynearJurjerewhairral.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly CLogger _cLogger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, CLogger cLogger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _cLogger = cLogger;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        [Route("/")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            //Console.WriteLine($"HttpContextAccessor={_httpContextAccessor.HttpContext.TraceIdentifier} HttpContext={HttpContext.TraceIdentifier} {_httpContextAccessor.HttpContext.TraceIdentifier.Equals(HttpContext.TraceIdentifier)} {Thread.GetCurrentProcessorId()}");

            await Task.Delay(TimeSpan.FromSeconds(10));

            _logger.Log(LogLevel.Information, $"HttpContext={HttpContext.TraceIdentifier} ");

            //Console.WriteLine($"HttpContextAccessor={_httpContextAccessor.HttpContext.TraceIdentifier} HttpContext={HttpContext.TraceIdentifier} {_httpContextAccessor.HttpContext.TraceIdentifier.Equals(HttpContext.TraceIdentifier)} {Thread.GetCurrentProcessorId()}");

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}

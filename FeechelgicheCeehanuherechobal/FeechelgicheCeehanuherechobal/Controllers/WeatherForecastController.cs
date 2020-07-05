using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FeechelgicheCeehanuherechobal.Controllers
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
        private readonly Foo _foo;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, Foo foo)
        {
            _logger = logger;
            _foo = foo;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();

            _logger.Info("123");

            try
            {
                throw new ArgumentException("123");
            }
            catch (Exception e)
            {
                _logger.Info("f", e, tags: "12");
            }

            _logger.Debug("Debug");
            _logger.Warning("Warning");
            _logger.Error("f");

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
                .ToArray();
        }
    }

    public class Foo
    {
        public Foo(ILogger<Foo> logger)
        {
            _logger = logger;
        }

        private readonly ILogger<Foo> _logger;
    }
}
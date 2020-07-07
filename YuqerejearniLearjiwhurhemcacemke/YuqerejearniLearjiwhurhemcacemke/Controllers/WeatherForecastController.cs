using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace YuqerejearniLearjiwhurhemcacemke.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        public WeatherForecastController(ILogger<WeatherForecastController> logger, F1 f1, Info info,
            IServiceProvider serviceProvider)
        {
            Info = info;
            _logger = logger;
            _f1 = f1;
            _serviceProvider = serviceProvider;
        }

        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public Info Info { get; }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            Info.Id = DateTime.Now.ToString();
            _logger.LogInformation(Info.Id);
            _f1.Do();

            var f3 = _serviceProvider.GetService<F3>();
            f3.Do();

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
                .ToArray();
        }

        private readonly F1 _f1;

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IServiceProvider _serviceProvider;
    }
}
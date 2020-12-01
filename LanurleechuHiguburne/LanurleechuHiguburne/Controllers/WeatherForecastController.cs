using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartSql.Exceptions;

namespace LanurleechuHiguburne.Controllers
{
    [ApiController]
    [Route("/")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IUserRepository _userRepository;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IUserRepository  userRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
        }

        [HttpGet]
        public string Get()
        {
            return "";
        }
    }

    public class Properties
    {
        private readonly IDictionary<string, string> _properties;
        private readonly Regex _propertyTokens;

        public Properties()
        {
            _properties = new Dictionary<string, string>();
            var regOptions = RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled;
            _propertyTokens = new Regex(@"\$\{([\p{L}\p{N}_\W]+)\}", regOptions);
        }

        public void Import(IEnumerable<KeyValuePair<string, object>> properties)
        {
            foreach (var property in properties)
            {
                AddProperty(property.Key, property.Value.ToString());
            }
        }

        public void Import(IEnumerable<KeyValuePair<string, string>> properties)
        {
            foreach (var property in properties)
            {
                AddProperty(property.Key, property.Value);
            }
        }

        public void AddProperty(string key, string value)
        {
            var propertyVal = GetPropertyValue(value);
            _properties.Add(key, propertyVal);
        }

        public string GetPropertyValue(string propExp)
        {
            if (String.IsNullOrEmpty(propExp))
            {
                return propExp;
            }
            if (!_propertyTokens.IsMatch(propExp))
            {
                return propExp;
            }

            return _propertyTokens.Replace(propExp, match =>
            {
                string propName = match.Groups[1].Value;
                if (!_properties.TryGetValue(propName, out var propVal))
                {
                    throw new SmartSqlException($"can not find Property.Name:{propName}.");
                }

                return propVal;
            });
        }
    }
}

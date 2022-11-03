using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace GemjabemrawWohearcebola;

internal partial class Program
{
    static void Main(string[] args)
    {
        string jsonString =
            @"{
  ""Date"": ""2019-09-01T00:00:00"",
  ""TemperatureCelsius"": 25,
  ""Summary"": ""Hot""
}
";
        WeatherForecast? weatherForecast;

        weatherForecast = JsonSerializer.Deserialize<WeatherForecast>(
            jsonString, MyJsonContext.Default.WeatherForecast);
        Console.WriteLine($"Date={weatherForecast?.Date}");

        jsonString = JsonSerializer.Serialize(
            weatherForecast!, MyJsonContext.Default.WeatherForecast);
        Console.WriteLine(jsonString);
    }

    public class WeatherForecast
    {
        public DateTime Date { get; set; }
        public int TemperatureCelsius { get; set; }
        public string? Summary { get; set; }
    }

    [JsonSerializableAttribute(typeof(Foo))]
    [JsonSerializableAttribute(typeof(WeatherForecast))]
    internal partial class MyJsonContext : JsonSerializerContext
    {

    }
}

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace YaycearnojeliYereqemwayewhall
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient();
            await httpClient.GetAsync("http://localhost:5000/WeatherForecast");
        }
    }
}

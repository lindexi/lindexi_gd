using System;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace BedenairfejowokoHileballyee
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var url = "http://127.0.0.1:5673";

            var httpClient = new HttpClient();
            for (int i = 0; i < 100000000; i++)
            {
                Console.WriteLine($"{i}");

                _ = httpClient.GetAsync(url).ContinueWith(async (t) =>
                  {
                      var httpResponseMessage = await t;
                      Console.WriteLine(httpResponseMessage.StatusCode);
                  });

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            Console.Read();
        }
    }
}

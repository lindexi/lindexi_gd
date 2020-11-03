using System;
using System.Net.Http;
using System.Threading;
using YuqerejearniLearjiwhurhemcacemke;

namespace WeakebawoLelelcejee
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var httpClient = new HttpClient();
            while (true)
            {
                try
                {
                    var str = httpClient.GetStringAsync($"{Context.Url}/weatherforecast").Result;
                    Console.WriteLine(str);

                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                }
            }
        }
    }
}
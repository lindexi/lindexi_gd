using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace RernallkarhadahiNearlaynerene
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var httpClient = new HttpClient();

            var str = await httpClient.GetStringAsync("http://pv.sohu.com/cityjson");

            Console.WriteLine(str); // var returnCitySN = {"cip": "183.63.127.82", "cid": "440100", "cname": "广东省广州市"};

            var regex = new Regex(@"(\d+\.\d+\.\d+\.\d+)");
            var match = regex.Match(str);
            if (match.Success)
            {
                var ip = match.Groups[0].Value;
                Console.WriteLine(ip);
            }

            Console.Read();
        }
    }
}

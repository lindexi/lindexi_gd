using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GujidearkiLicairfeeka
{
    class Program
    {
        static void Main(string[] args)
        {
            var list = new List<string>();
            Task.Run(() =>
            {
                while (true)
                {
                    var str = list.FirstOrDefault();
                    Console.WriteLine(str);
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    list.Clear();
                    list.Add("doubi");
                }
            });

            Console.Read();
        }
    }
}

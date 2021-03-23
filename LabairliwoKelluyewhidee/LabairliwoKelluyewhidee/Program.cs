using System;
using System.Collections.Generic;
using System.Linq;

namespace LabairliwoKelluyewhidee
{
    class Program
    {
        static void Main(string[] args)
        {
            var dateTimeList = new List<DateTime>()
            {
                DateTime.Now,
                DateTime.Now.AddHours(1),
                DateTime.Now.AddHours(2),
            };

            foreach (var dateTime in dateTimeList.OrderBy(temp => temp))
            {
                Console.WriteLine(dateTime);
            }
        }
    }
}
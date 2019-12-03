using System;
using System.Collections.Generic;
using System.Linq;

namespace Gacwhecruinlkreeu
{
    class Program
    {
        static void Main(string[] args)
        {
            var list = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(i);
            }

            foreach (var temp in list.Take(100))
            {
                Console.WriteLine(temp);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JekigaranekiferLaijolellecho
{
    class Program
    {
        static void Main(string[] args)
        {
            var list = new List<int>() { 1, 2, 5, 3, 2, 4, 6, 2, 21, 45 };
            var numberList = new Dictionary<int, int>();
            foreach (var temp in list)
            {
                if (numberList.ContainsKey(temp))
                {
                    numberList[temp]++;
                }
                else
                {
                    numberList[temp] = 1;
                }
            }

            Console.WriteLine("存在的众数");

            foreach (var temp in numberList.Where(pair => pair.Value>1).Select(pair=>new {Number=pair.Key,Count=pair.Value}).OrderByDescending(temp=>temp.Count))
            {
                Console.WriteLine($"元素 {temp.Number} 出现次数 {temp.Count}");
            }

            Console.Read();
        }
    }
}

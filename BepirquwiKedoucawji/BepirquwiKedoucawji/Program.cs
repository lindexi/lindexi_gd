using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
// ReSharper disable All

namespace BepirquwiKedoucawji
{
    class Program
    {
        static void Main(string[] args)
        {
            var (name, count) = new List<int>() { 1, 2, 3 };
            Console.WriteLine($"{name} {count}");
        }
    }


    static class Extension
    {
        public static void Deconstruct(this List<int> list, out string name, out int count)
        {
            name = string.Join(",", list);
            count = list.Count;
        }
    }
}
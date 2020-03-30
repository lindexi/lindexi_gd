using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var lindexi = new Lindexi();
            var (name, doubi) = lindexi;
            Console.WriteLine($"{name} {doubi}");
        }
    }

    class Lindexi
    {
        public string Name { get; } = "林德熙";
        public string Doubi { get; } = "逗比";

        public void Deconstruct(out string name, out string doubi)
        {
            name = Name;
            doubi = Doubi;
        }
    }
}
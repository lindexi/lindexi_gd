using System;
using System.Collections.Generic;
using TypeNameFormatter;

namespace LelerefurfeeKeakacheequr
{
    class Program
    {
        static void Main(string[] args)
        {
            var fType = typeof(List<F>);
            Console.WriteLine(typeof(List<int>));
            Console.WriteLine(fType.GetFormattedName());
        }
    }

    class F
    {
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
    }
}

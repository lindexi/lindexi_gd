using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WearbewhujawChallwealicel
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Process.GetCurrentProcess().Id);
            var list = new List<object>();
            while(true)
            {
                list.Add(new Lindexi());
                Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
            }
        }
    }

    class Lindexi
    {
        
    }
}

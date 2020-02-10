using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DalljalfonafearBairyekeregu
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\NarhedeachawhearWeargijawgowe.exe");
            Process.Start(file);
        }
    }
}

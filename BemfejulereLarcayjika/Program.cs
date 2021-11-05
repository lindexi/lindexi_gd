using System;
using System.IO;

namespace BemfejulereLarcayjika
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var a = Directory.CreateDirectory("aa");

            var result = Directory.CreateSymbolicLink("bbb", a.FullName) as DirectoryInfo;

            // 输出 bbb 文件夹
            Console.WriteLine(result.FullName);
        }
    }
}


using System;
using System.IO;

namespace BemfejulereLarcayjika
{
    public class Program
    {
        public static void Main(string[] args)
        {
            File.WriteAllText("a.txt", "123");

            var result = File.CreateSymbolicLink("b.txt", "a.txt") as FileInfo;

            // 输出 b 文件
            Console.WriteLine(result.FullName);

            Console.WriteLine(File.ReadAllText("b.txt"));
        }
    }
}


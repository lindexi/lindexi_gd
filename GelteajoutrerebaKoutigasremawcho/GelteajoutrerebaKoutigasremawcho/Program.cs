using System;
using System.IO;

namespace GelteajoutrerebaKoutigasremawcho
{
    class Program
    {
        static void Main(string[] args)
        {
            DirectoryInfo dir = new DirectoryInfo(@"GelteajoutrerebaKoutigasremawcho.dll");
            Console.WriteLine(dir.Exists);
        }
    }
}

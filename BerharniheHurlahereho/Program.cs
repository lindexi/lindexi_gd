using System;

namespace BerharniheHurlahereho
{
    class Program
    {
        static void Main(string[] args)
        {
            var type = typeof(Foo);

            foreach (var t in typeof(Program).Assembly.GetTypes())
            {
                Console.WriteLine(t.FullName);
            }
        }
    }

    class Foo
    {
        static Foo()
        {
            Console.WriteLine("Foo");
        }
    }
}

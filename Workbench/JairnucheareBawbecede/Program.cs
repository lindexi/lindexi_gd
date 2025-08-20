using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JairnucheareBawbecede
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var list = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(Foo());
            }

            foreach (var str in list)
            {
                Console.WriteLine(str);
            }
        }

        private static unsafe string Foo()
        {
            char* c = stackalloc char[10];
            for (int i = 0; i < 5; i++)
            {
                c[i] = 'a';
            }

            return new string(c);
        }
    }
}

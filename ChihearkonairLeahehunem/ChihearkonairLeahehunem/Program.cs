using System;
using System.Collections;
using System.Collections.Generic;

namespace ChihearkonairLeahehunem
{
    class Program
    {
        static void Main(string[] args)
        {
            var hashtable = new Hashtable();

            var list = new List<object>();

            for (int i = 0; i < 1000; i++)
            {
                list.Add(i);
            }

            for (int i = 0; i < 35; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    hashtable.Add(list[i * 10 + j], list[i]);
                }

                Console.WriteLine(i + " " + GC.GetAllocatedBytesForCurrentThread());
            }

            Console.WriteLine(hashtable.Count);
        }
    }
}

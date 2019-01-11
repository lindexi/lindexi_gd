using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace HeeSawhowyowjai
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ArrayFindAll>();
        }
    }


    public class ArrayFindAll
    {
        [Benchmark(Baseline = true)]
        public void FindAll()
        {
            var foo = new Foo[10000000];
            Array.Fill(foo, new Foo());

            foo = Array.FindAll(foo, _ => true);
        }

        [Benchmark()]
        public void FindAllWithLinkedList()
        {
            var foo = new Foo[10000000];
            Array.Fill(foo, new Foo());

            foo = FindAllWithLinkedList(foo, _ => true);
        }

        public T[] FindAllWithLinkedList<T>(T[] array, Predicate<T> match)
        {
            var list = new LinkedList<T>();
            for (int i = 0; i < array.Length; i++)
            {
                if (match(array[i]))
                {
                    list.AddLast(array[i]);
                }
            }

            return list.ToArray();
        }
    }

    class Foo
    {
    }
}
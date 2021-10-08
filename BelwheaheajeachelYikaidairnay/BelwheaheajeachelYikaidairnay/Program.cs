using System;
using System.Collections.Generic;
using System.Linq;

namespace BelwheaheajeachelYikaidairnay
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Foo[] foo = new Foo[100];
                for (int i = 0; i < 100; i++)
                {
                    foo[i] = new Foo(i.ToString());
                }

                Dictionary<Foo, string> dictionary = foo.ToDictionary(t => t, t => t.Name);

                for (int i = 0; i < foo.Length; i++)
                {
                    Console.WriteLine($"{foo[i].Name}-{dictionary[foo[i]]}"); // KeyNotFoundException
                }
            }
            catch (Exception)
            {
            }

            try
            {
                var foo2 = new Foo2();
                Dictionary<Foo2, object> dictionary = new();
                dictionary[foo2] = foo2;
                Console.WriteLine(dictionary.ContainsKey(foo2));
                foo2.HashCode = 2;
                Console.WriteLine(dictionary.ContainsKey(foo2));
            }
            catch (Exception)
            {
            }
        }
    }

    class Foo
    {
        public Foo(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override int GetHashCode()
        {
            return _random.Next();
        }

        private readonly Random _random = new Random();
    }

    class Foo2
    {
        public int HashCode { set; get; }

        public override int GetHashCode()
        {
            return HashCode;
        }
    }
}

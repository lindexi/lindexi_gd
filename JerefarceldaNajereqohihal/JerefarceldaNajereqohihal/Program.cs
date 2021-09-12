using System;

namespace JerefarceldaNajereqohihal
{
    class Program
    {
        static void Main(string[] args)
        {
            Action<object> foo = Foo;
            object o = foo;
            var f1 = o as F1;

        }

        private static void Foo(object obj)
        {
            
        }
    }

    delegate void F1(object obj);
    delegate void F2(object obj);
}

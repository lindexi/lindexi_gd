using System;
using System.Collections.Generic;
using System.Linq;

namespace HurnilecelrewhuCucijarkeljerela
{
    class Program
    {
        static void Main(string[] args)
        {
            object[] foo = new List<string>
            {
                "lindexi",
                "欢迎访问我博客 https://blog.lindexi.com/ 里面有大量 UWP WPF 博客"
            }.ToArray<object>();

            object[] f1 = foo;

            f1[1] = 10;

            Foo(foo);
        }

        private static void Foo(object[] obj)
        {
            obj[0] = 10;
        }
    }
}
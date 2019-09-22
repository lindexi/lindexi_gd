using System;
using System.Runtime.InteropServices;

namespace CeneewayhiGechacurjearfeferlel
{
    class Program
    {
        static void Main(string[] args)
        {
            var foo = new IFoo();
            Console.WriteLine(foo.Foo());
        }
    }

    [ComImport]
    [CoClass(typeof(Foo))]
    [Guid("5742D257-CCCC-4F7A-8191-6362609C458D")]
    public interface IFoo
    {
        /// <summary>
        /// 有趣方法
        /// </summary>
        /// <returns></returns>
        string Foo();
    }

    [ComImport]
    [Guid("5742D257-CCCC-4F7A-8191-6362609C458D")]
    internal class Foo
    {
    }
}

using System;
using System.Reflection;

namespace NaleneenurYalahewe
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Foo<IF1>.GetObject().F2());
        }
    }

    interface IF1
    {
        string F2();
    }


    public class Foo<T> : DispatchProxy
    {
        public static T GetObject()
        {
            return DispatchProxy.Create<T, Foo<T>>();
        }

        /// <inheritdoc />
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            return "lindexi";
        }
    }
}

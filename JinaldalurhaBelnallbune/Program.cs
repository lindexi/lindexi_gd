using System;
using System.Runtime.Serialization;

namespace JinaldalurhaBelnallbune
{
    class Program
    {
        static void Main(string[] args)
        {
            Foo();
        }

        private static void Foo()
        {
            var f1 = FormatterServices.GetUninitializedObject(typeof(F1));
        }
    }
}

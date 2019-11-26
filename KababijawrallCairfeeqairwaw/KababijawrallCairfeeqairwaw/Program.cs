using System;
using System.Reflection;

namespace KababijawrallCairfeeqairwaw
{
    class Program
    {
        static void Main(string[] args)
        {
            Foo f1 = new F1();

            Console.WriteLine(f1.IsOverride());

            f1 = new Foo();
            Console.WriteLine(f1.IsOverride());
        }
    }

    class Foo
    {
        public bool IsOverride()
        {
            var methodInfo = GetType().GetMethod("Test");
            if (methodInfo != methodInfo.GetBaseDefinition())
            {

            }

            return !(GetType().GetMethod("Test").DeclaringType == typeof(Foo));
        }

        public virtual void Test()
        {

        }
    }

    class F1 : Foo
    {
        /// <inheritdoc />
        public override void Test()
        {
        }
    }
}

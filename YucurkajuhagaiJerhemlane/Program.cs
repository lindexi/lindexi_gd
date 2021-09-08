using System;

namespace YucurkajuhagaiJerhemlane
{
    class Program
    {
        static void Main(string[] args)
        {
            var foo = Foo.F2;
            NukerohoheQawhallnerwalni(args, foo);
        }

        private static void NukerohoheQawhallnerwalni(string[] args, Foo foo)
        {
            while (args.Length == 0)
            {
                if (foo == Foo.F1)
                {
                }
            }
        }
    }

    enum Foo
    {
        F1,
        F2
    }
}

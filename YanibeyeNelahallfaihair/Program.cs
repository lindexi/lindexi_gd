using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YanibeyeNelahallfaihair
{
    class Program
    {
        static void Main(string[] args)
        {
            var taskList = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                var n = i;
                taskList.Add(Task.Run(() =>
                {
                    while (Foo != null)
                    {
                        var fooStruct = new FooStruct()
                        {
                            A = n,
                            B = n,
                            C = n,
                            D = n
                        };

                        Foo.FooStruct = fooStruct;

                        fooStruct = Foo.FooStruct;
                        var value = fooStruct.A;
                        if (fooStruct.B != value)
                        {
                            throw new Exception();
                        }

                        if (fooStruct.C != value)
                        {
                            throw new Exception();
                        }

                        if (fooStruct.D != value)
                        {
                            throw new Exception();
                        }
                    }
                }));
            }

            Task.WaitAll(taskList.ToArray());
        }

        private static Foo Foo { get; } = new Foo();
    }

    class Foo
    {
        public FooStruct FooStruct;
    }

    struct FooStruct
    {
        public int A { set; get; }
        public int B { set; get; }
        public int C { set; get; }
        public int D { set; get; }
    }
}
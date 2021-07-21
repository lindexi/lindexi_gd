using System;

namespace FairhojafallJeeleefuyi
{
    class Program
    {
        static void Main(string[] args)
        {
            Action action = Foo;
            for (int i = 0; i < 10; i++)
            {
                action += Foo;
            }

            for (int i = 0; i < 100; i++)
            {
                var beforeAllocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
                var invocationList = action.GetInvocationList();
                var afterAllocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
                Console.WriteLine(afterAllocatedBytesForCurrentThread - beforeAllocatedBytesForCurrentThread);
            }

            Console.Read();

            static void Foo()
            {

            }
        }
    }
}

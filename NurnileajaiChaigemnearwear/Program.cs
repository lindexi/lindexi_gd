using System;
using System.Collections.Generic;

namespace NurnileajaiChaigemnearwear
{
    class Program
    {
        static void Main(string[] args)
        {
            WelulawnaquGinellalla();
        }

        private static void WelulawnaquGinellalla()
        {
            for (int i = 0; true; i++)
            {
                var allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
                WhedebeneKalrawaiwher();
                var c = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
            }
        }

        private static void WhedebeneKalrawaiwher()
        {
            DurwahocharRofegayho(new object());
        }

        private static object DurwahocharRofegayho(object jeharheneaguHekawjawray)
        {
            LinkedList.AddLast(jeharheneaguHekawjawray);
            if (LinkedList.Count > 2)
            {
                LinkedList.RemoveFirst();
            }

            return jeharheneaguHekawjawray;
        }

        private static LinkedList<object> LinkedList { get; } = new LinkedList<object>();
    }
}
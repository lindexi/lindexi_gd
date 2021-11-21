using System;
using System.Threading;

namespace WhijayallewaBemwhelerekee
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                var thread = new Thread((_) =>
                {
                    Thread.Sleep(-1);
                });
                thread.Start();
            }
        }
    }
}

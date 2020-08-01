using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Number;

namespace HaifeljiweajeYeelarkerjairere
{
    class Program
    {
        static void Main(string[] args)
        {
            var autoResetEvent = new AutoResetEvent(false);

            Foo(autoResetEvent);

            autoResetEvent.Set();

            for (int i = 0; i < 10; i++)
            {
                autoResetEvent.Set();
                Thread.Sleep(100);
            }

            Console.Read();
        }

        private static void Foo(AutoResetEvent autoResetEvent)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    autoResetEvent.WaitOne();

                    Console.WriteLine("Foo");
                }
            });
        }
    }
}
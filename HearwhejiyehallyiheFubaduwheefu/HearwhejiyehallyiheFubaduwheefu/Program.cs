using System;
using System.ComponentModel;
using System.Threading;

namespace HearwhejiyehallyiheFubaduwheefu
{
    class Program
    {
        static void Main(string[] args)
        {
            var categoryAttribute = new CategoryAttribute();

            var manualResetEvent = new ManualResetEvent(false);

            string[] categoryList = new string[100];
            var threadList = new Thread[100];
            for (int i = 0; i < 100; i++)
            {
                var n = i;
                threadList[n] = new Thread(() =>
                {
                    manualResetEvent.WaitOne();
                    categoryList[n] = categoryAttribute.Category;
                });
            }

            for (int i = 0; i < 100; i++)
            {
                threadList[i].Start();
            }

            manualResetEvent.Set();

            for (int i = 0; i < 100; i++)
            {
                threadList[i].Join();
            }

            string category = categoryList[0];
            for (int i = 1; i < 100; i++)
            {
                if (!ReferenceEquals(category, categoryList[i]))
                {

                }
            }
        }
    }
}

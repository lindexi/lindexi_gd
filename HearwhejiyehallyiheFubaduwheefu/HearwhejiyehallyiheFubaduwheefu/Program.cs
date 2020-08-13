using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace HearwhejiyehallyiheFubaduwheefu
{
    class Program
    {
        static void Main(string[] args)
        {
            var component = new Component();
            lock (component)
            {
                Task.Run(() => component.Dispose()).Wait();
            }
        }
    }
}

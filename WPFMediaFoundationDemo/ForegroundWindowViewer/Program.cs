using Lsj.Util.Win32;
using Lsj.Util.Win32.NativeUI;
using System;
using System.Threading;

namespace ForegroundWindowViewer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                var win = new Win32Window(User32.GetForegroundWindow());
                Console.WriteLine($"{win.Handle} {win.Text}");

                Thread.Sleep(100);

            }
        }
    }
}

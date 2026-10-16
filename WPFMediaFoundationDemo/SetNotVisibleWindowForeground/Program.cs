using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.NativeUI;
using System;
using System.Threading;

namespace SetNotVisibleWindowForeground
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var uiThreadManager = new UIThreadManager();
            _ = uiThreadManager.InvokeAsync(() =>
            {
                //Thread.Sleep(2000);

                var win = new TestWindow();
                var handle = win.Handle;

                User32.SetForegroundWindow(handle);
                var current = User32.GetForegroundWindow();


            });

            Console.ReadLine();
        }

        class TestWindow : Win32Window
        {
            public TestWindow() : base("", parentWindow: Constants.HWND_MESSAGE)
            {

            }

            protected override LRESULT WindowProc(HWND hWnd, WindowMessages msg, WPARAM wParam, LPARAM lParam)
            {
                if (msg == WindowMessages.WM_KEYDOWN)
                {
                    Console.WriteLine("Key Down");
                }
                return base.WindowProc(hWnd, msg, wParam, lParam);
            }
        }
    }
}

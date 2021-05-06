using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using PInvoke;

namespace NejelwaqaifagakoNearkaceabeday
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var application = new Application();
            var window = new Window();
            window.Loaded += Window_Loaded;
            application.Run(window);
        }

        private static void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var WM_SYSCOMMAND = 0x0112;
            var SC_MONITORPOWER = 0xF170;

            var MonitorShutoff =1;

            var HWND_BROADCAST = User32.GetDesktopWindow();

            Thread.Sleep(1000);

            User32.SendMessage(HWND_BROADCAST, User32.WindowMessage.WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MonitorShutoff);

            var windowInteropHelper = new WindowInteropHelper((Window)sender);
            User32.SendMessage(windowInteropHelper.Handle, User32.WindowMessage.WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MonitorShutoff);
        }
    }
}

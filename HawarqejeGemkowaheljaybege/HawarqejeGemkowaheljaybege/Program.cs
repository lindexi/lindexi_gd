using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace HawarqejeGemkowaheljaybege
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var application = new Application();
            application.Startup += (sender, eventArgs) =>
            {
                var window = new Window();
                window.Loaded += Window_Loaded;
                window.Show();
            };
            application.Run();
        }

        private static void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = GetOpenClipboardWindow();
            Console.WriteLine($"占用剪贴板 HWND={hwnd} LastError={Marshal.GetLastWin32Error()}");
        }

        [DllImport("User32", SetLastError = true)]
        extern static int GetOpenClipboardWindow();
    }
}

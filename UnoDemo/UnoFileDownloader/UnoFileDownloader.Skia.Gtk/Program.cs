using System;
using System.Runtime.InteropServices;
using GLib;

using Uno.UI.Runtime.Skia.Gtk;

namespace UnoFileDownloader.Skia.Gtk
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
            {
                Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
                expArgs.ExitApplication = true;
            };

            var host = new GtkHost(() => new AppHead());

            // 防止虚拟机内闪烁
            host.RenderSurfaceType = RenderSurfaceType.Software;

            host.Run();
        }
    }
}

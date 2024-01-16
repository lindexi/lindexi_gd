using System;
using Windows.Foundation;
using GLib;
using Microsoft.UI.Xaml;
using Uno.UI.Runtime.Skia.Gtk;
using SamplesApp;

namespace LacebayjeejiBehebilawla.Skia.Gtk;
public class Program
{
    public static void Main(string[] args)
    {
        ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
        {
            Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
            expArgs.ExitApplication = true;
        };

        WindowHelper.WindowActivator = new WindowActivator();

        var host = new GtkHost(() => new AppHead());

        host.Run();
    }

    class WindowActivator : IWindowActivator
    {
        public void Resize(Window window, Size size)
        {
            var nativeWindow = (global::Gtk.Window) window.GetNativeWindow();
            nativeWindow.Resize((int) size.Width, (int) size.Height);
        }
    }
}

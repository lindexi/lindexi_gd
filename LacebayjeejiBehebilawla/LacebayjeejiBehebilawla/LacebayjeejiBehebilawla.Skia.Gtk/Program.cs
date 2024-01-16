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

        var windowActivator = new WindowActivator();
        WindowHelper.WindowActivator = windowActivator;

        var host = new GtkHost(() =>
        {
            var app = new AppHead();
            return app;
        });
        windowActivator.GtkHost = host;
        host.Run();
    }

    class WindowActivator : IWindowActivator
    {
        public GtkHost GtkHost { get; set; } = null!;

        public void ResizeMainWindow(Size size)
        {
            var nativeWindow = GtkHost.Window;

            nativeWindow.Resize((int) size.Width, (int) size.Height);
        }
    }
}

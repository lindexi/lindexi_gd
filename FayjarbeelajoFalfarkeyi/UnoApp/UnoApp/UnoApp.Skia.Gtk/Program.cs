using GLib;

using System;
using Gdk;
using Gtk;
using Uno.UI.Runtime.Skia;

namespace UnoApp.Skia.Gtk;
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
        {
            Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
            expArgs.ExitApplication = true;
        };

        var host = new GtkHost(() =>
        {
            var appHead = new AppHead();
            var window = GtkHost.Window;
            window.Decorated = false;

            window.SetDefaultSize(300, 200);
            appHead.Launched += (sender, eventArgs) =>
            {
                window.SetDefaultSize(300, 200);
                window.SetSizeRequest(300, 200);
                window.Child.SetSizeRequest(300, 200);
                window.SizeAllocate(new Rectangle(0, 0, 300, 200));
                window.GetSize(out var w, out var h);
            };
            return appHead;
        }, args);
        host.RenderSurfaceType = RenderSurfaceType.Software;

        host.Run();


    }
}

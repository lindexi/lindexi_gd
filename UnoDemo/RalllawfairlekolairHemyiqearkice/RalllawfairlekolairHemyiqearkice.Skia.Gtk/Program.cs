using System;

using GLib;

using Uno.UI.Runtime.Skia.Gtk;

namespace RalllawfairlekolairHemyiqearkice.Skia.Gtk;
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
        host.RenderSurfaceType = RenderSurfaceType.Software;
        PlatformHelper.PlatformProvider = new GtkPlatformProvider(host);
        host.Run();
    }
}

public class GtkPlatformProvider : IPlatformProvider
{
    public GtkPlatformProvider(GtkHost gtkHost)
    {
        _gtkHost = gtkHost;
    }

    private readonly GtkHost _gtkHost;

    public void EnterFullScreen()
    {
        _gtkHost.Window?.Fullscreen();
    }

    public void ExitFullScreen()
    {
        _gtkHost.Window?.Unfullscreen();
    }
}

using System;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

using GLib;

using Uno.UI.Runtime.Skia.Gtk;

namespace RalllawfairlekolairHemyiqearkice.Skia.Gtk;
public class Program
{
    public static void Main(string[] args)
    {
        AssemblyLoadContext.Default.ResolvingUnmanagedDll += (assembly, s) =>
        {
            if (s is "libSkiaSharp" or "libHarfBuzzSharp.dll")
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {

                }
            }
            return IntPtr.Zero;
        };

        ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
        {
            Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
            expArgs.ExitApplication = true;
        };

        var host = new GtkHost(() => new AppHead());

        host.Run();
    }


}

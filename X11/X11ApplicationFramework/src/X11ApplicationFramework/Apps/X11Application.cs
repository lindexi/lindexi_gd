using System;
using System.Runtime.Versioning;
using System.Text;

using static X11ApplicationFramework.Natives.XLib;

namespace X11ApplicationFramework.Apps;

[SupportedOSPlatform("Linux")]
public class X11Application
{
    public X11Application()
    {
        var display = XOpenDisplay(IntPtr.Zero);
        var screen = XDefaultScreen(display);

        if (XCompositeQueryExtension(display, out var eventBase, out var errorBase) == 0)
        {
            Console.WriteLine("Error: Composite extension is not supported");
            XCloseDisplay(display);
            throw new NotSupportedException("Error: Composite extension is not supported");
        }
        else
        {
            //Console.WriteLine("XCompositeQueryExtension");
        }

        var rootWindow = XDefaultRootWindow(display);

        var x11Info = new X11InfoManager(display, screen, rootWindow);

        X11Info = x11Info;
    }

    public X11InfoManager X11Info { get; }

    internal void RegisterWindow(X11Window window)
    {
        WindowManager.RegisterWindow(window);
    }

    internal X11WindowManager WindowManager { get; } = new X11WindowManager();

    public void Run()
    {

    }
}
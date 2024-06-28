using CPF.Linux;
using System;
using System.Diagnostics;
using System.Runtime;
using static CPF.Linux.XLib;

XInitThreads();
var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);

var win1 = new X11Window(display);
var win2 = new X11Window(display);

XMapWindow(display, win1.Window);
XMapWindow(display, win2.Window);

XSetTransientForHint(display, win1.Window, win2.Window);

XFlush(display);

var white = XWhitePixel(display, screen);
var black = XBlackPixel(display, screen);

XSync(display, false);

Task.Run(() =>
{
    Console.ReadLine();
    Console.WriteLine("unmap win1");
    XUnmapWindow(display, win1.Window);
    XFlush(display);

    Console.ReadLine();
    Console.WriteLine("map win1");
    XMapWindow(display, win1.Window);
    XFlush(display);

    Console.ReadLine();
    Console.WriteLine("unmap win2");
    XUnmapWindow(display, win2.Window);
    XFlush(display);

    Console.ReadLine();
    Console.WriteLine("map win2");
    XMapWindow(display, win2.Window);
    XFlush(display);

    Console.ReadLine();
    Console.WriteLine("Re set owner");
    XSetTransientForHint(display, win1.Window, win2.Window);
    XFlush(display);
});

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);
}

class X11Window
{
    public X11Window(IntPtr display)
    {
        var screen = XDefaultScreen(display);

        var rootWindow = XDefaultRootWindow(display);

        XMatchVisualInfo(display, screen, 32, 4, out var info);
        var visual = info.visual;

        var valueMask =
                //SetWindowValuemask.BackPixmap
                0
                | SetWindowValuemask.BackPixel
                | SetWindowValuemask.BorderPixel
                | SetWindowValuemask.BitGravity
                | SetWindowValuemask.WinGravity
                | SetWindowValuemask.BackingStore
                | SetWindowValuemask.ColorMap
            //| SetWindowValuemask.OverrideRedirect
            ;
        var xSetWindowAttributes = new XSetWindowAttributes
        {
            backing_store = 1,
            bit_gravity = Gravity.NorthWestGravity,
            win_gravity = Gravity.NorthWestGravity,
            //override_redirect = true, // 设置窗口的override_redirect属性为True，以避免窗口管理器的干预
            colormap = XCreateColormap(display, rootWindow, visual, 0),
            border_pixel = 0,
            background_pixel = BackgroundColorIntPtr[_index] //new IntPtr(Random.Shared.Next() | 0xFF << 24),
        };

        _index++;

        var xDisplayWidth = XDisplayWidth(display, screen) / 2;
        var xDisplayHeight = XDisplayHeight(display, screen) / 2;
        var window = XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
            32,
            (int)CreateWindowArgs.InputOutput,
            visual,
            (nuint)valueMask, ref xSetWindowAttributes);

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int)ignoredMask);
        XSelectInput(display, window, mask);

        var white = XWhitePixel(display, screen);
        var black = XBlackPixel(display, screen);

        var gc = XCreateGC(display, window, 0, 0);
        XSetForeground(display, gc, white);
        XSync(display, false);

        Window = window;
        GC = gc;
    }

    public IntPtr Window { get; set; }
    public IntPtr GC { get; set; }

    private static IntPtr[] BackgroundColorIntPtr { get; } =
    [
        new IntPtr(0xFFFF0000),
        new IntPtr(0xFF00FF00),
        new IntPtr(0xFF0000FF),
    ];

    private static int _index;
}
using System;
using CPF.Linux;

namespace RaynilequKuharrurnudoqer;

class X11Window
{
    public X11Window(IntPtr display)
    {
        var screen = XLib.XDefaultScreen(display);

        var rootWindow = XLib.XDefaultRootWindow(display);

        XLib.XMatchVisualInfo(display, screen, 32, 4, out var info);
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
            colormap = XLib.XCreateColormap(display, rootWindow, visual, 0),
            border_pixel = 0,
            //background_pixel = BackgroundColorIntPtr[_index] //new IntPtr(Random.Shared.Next() | 0xFF << 24),
            background_pixel = 0,
        };

        _index++;

        var xDisplayWidth = XLib.XDisplayWidth(display, screen) / 2;
        var xDisplayHeight = XLib.XDisplayHeight(display, screen) / 2;
        var window = XLib.XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
            32,
            (int) CreateWindowArgs.InputOutput,
            visual,
            (nuint) valueMask, ref xSetWindowAttributes);

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XLib.XSelectInput(display, window, mask);

        var white = XLib.XWhitePixel(display, screen);
        var black = XLib.XBlackPixel(display, screen);

        var gc = XLib.XCreateGC(display, window, 0, 0);
        XLib.XSetForeground(display, gc, white);
        XLib.XSync(display, false);

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
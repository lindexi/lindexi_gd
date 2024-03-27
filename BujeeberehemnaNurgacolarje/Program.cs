using System.Collections.Immutable;

using BlankX11App.X11;

using static BlankX11App.X11.XLib;
using static BlankX11App.X11.GlxConsts;
using System.Diagnostics;

//while (true)
//{
//    Thread.Sleep(100);
//    if (Debugger.IsAttached)
//    {
//        break;
//    }
//}

var display = XOpenDisplay(0);
var defaultScreen = XDefaultScreen(display);
var rootWindow = XRootWindow(display, defaultScreen);
//var visual = GetVisual(XOpenDisplay(0), defaultScreen);
var visual = IntPtr.Zero;

XMatchVisualInfo(display, defaultScreen, 32, 4, out var info);
visual = info.visual;

var valueMask = SetWindowValuemask.BackPixmap
    | SetWindowValuemask.BackPixel
    | SetWindowValuemask.BorderPixel
    | SetWindowValuemask.BitGravity
    | SetWindowValuemask.WinGravity
    | SetWindowValuemask.BackingStore
    | SetWindowValuemask.ColorMap;
var attr = new XSetWindowAttributes
{
    backing_store = 1,
    bit_gravity = Gravity.NorthWestGravity,
    win_gravity = Gravity.NorthWestGravity,
    override_redirect = 0,  // 参数：_overrideRedirect
    colormap = XCreateColormap(display, rootWindow, visual, 0),
};

var handle = XCreateWindow(display, rootWindow, 100, 100, 320, 240, 0,
    32,
    (int)CreateWindowArgs.InputOutput,
    visual,
    (nuint)valueMask, ref attr);
XMapWindow(display, handle);
XFlush(display);

while (XNextEvent(display, out var xEvent) == default)
{
}

XUnmapWindow(display, handle);
XDestroyWindow(display, handle);

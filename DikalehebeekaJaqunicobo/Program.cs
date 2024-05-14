// See https://aka.ms/new-console-template for more information

using CPF.Linux;
using System;
using System.Diagnostics;
using System.Runtime;
using static CPF.Linux.XLib;

var display = XOpenDisplay(IntPtr.Zero);
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
    background_pixel = 0,
};

var xDisplayWidth = XDisplayWidth(display, screen) / 2;
var xDisplayHeight = XDisplayHeight(display, screen) / 2;
var windowHandle = XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
    32,
    (int)CreateWindowArgs.InputOutput,
    visual,
    (nuint)valueMask, ref xSetWindowAttributes);


XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int)ignoredMask);
XSelectInput(display, windowHandle, mask);

XMapWindow(display, windowHandle);
XFlush(display);

var white = XWhitePixel(display, screen);
var black = XBlackPixel(display, screen);

var gc = XCreateGC(display, windowHandle, 0, 0);
XSetForeground(display, gc, white);
XSync(display, false);

_ = Task.Run(async () =>
{
    while (true)
    {
        var display1 = XOpenDisplay(IntPtr.Zero);
        var screen1 = XDefaultScreen(display1);
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var result = XIconifyWindow(display1, windowHandle, screen1);
            Console.WriteLine($"XIconifyWindow {result}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            XCloseDisplay(display1);
        }
    }
});

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);
    if (xNextEvent != 0)
    {
        Console.WriteLine($"xNextEvent {xNextEvent}");
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        XDrawLine(display, windowHandle, gc, 0, 0, 100, 100);
    }
}

Console.WriteLine("Hello, World!");
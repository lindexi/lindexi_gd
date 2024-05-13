// See https://aka.ms/new-console-template for more information

using CPF.Linux;
using System;
using static CPF.Linux.XLib;

var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);

var black = XBlackPixel(display, screen);

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
var handle = XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
    32,
    (int)CreateWindowArgs.InputOutput,
    visual,
    (nuint)valueMask, ref xSetWindowAttributes);


var window1 = new FooWindow(handle, display);


var window2 = new FooWindow(XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes), display);


//XSetInputFocus(Display, Window, 0, IntPtr.Zero);

XSync(display, false);

Task.Run(() => { });

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);
    if (xNextEvent != 0)
    {
        break;
    }

    if (@event.type == XEventName.MotionNotify)
    {
        var x = @event.MotionEvent.x;
        var y = @event.MotionEvent.y;

        if (@event.MotionEvent.window == window1.Window)
        {
            XDrawLine(display, window1.Window, window1.GC, x, y, x + 100, y);
        }
        else
        {
            var xEvent = new XEvent
            {
                MotionEvent =
                {
                    type = XEventName.MotionNotify,
                    send_event = true,
                    window = window1.Window,
                    display = display,
                    x = x,
                    y = y
                }
            };
            XSendEvent(display, window1.Window, propagate: false, new IntPtr((int)(EventMask.ButtonMotionMask)),
                ref xEvent);
        }
    }
}

Console.WriteLine("Hello, World!");

class FooWindow
{
    public FooWindow(nint windowHandle, nint display)
    {
        Window = windowHandle;

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int)ignoredMask);
        XSelectInput(display, windowHandle, mask);

        XMapWindow(display, windowHandle);
        XFlush(display);

        var screen = XDefaultScreen(display);
        var white = XWhitePixel(display, screen);

        var gc = XCreateGC(display, windowHandle, 0, 0);
        XSetForeground(display, gc, white);

        GC = gc;
    }

    public nint Window { get; }
    public IntPtr GC { get; }
}
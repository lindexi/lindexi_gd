// See https://aka.ms/new-console-template for more information

using CPF.Linux;

using System;
using System.Diagnostics;
using System.Runtime;

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
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes);


var window1 = new FooWindow(handle, display);
XSync(display, false);

IntPtr window2Handle = IntPtr.Zero;
IntPtr window2GCHandle = IntPtr.Zero;

if (args.Length == 0)
{
    var currentProcess = Process.GetCurrentProcess();
    var mainModuleFileName = currentProcess.MainModule!.FileName;
    //Process.Start(mainModuleFileName, [window1.Window.ToString(), window1.GC.ToString()]);

    _ = Task.Run(async () =>
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var result = XIconifyWindow(display, window1.Window, screen);
            Console.WriteLine($"XIconifyWindow {result}");
            await Task.Delay(TimeSpan.FromSeconds(1));
            XMapWindow(display, window1.Window);
        }
    });
}
else if (args.Length == 2)
{
    if (long.TryParse(args[0], out var otherProcessWindowHandle))
    {
        window2Handle = new IntPtr(otherProcessWindowHandle);
    }

    //if (long.TryParse(args[1], out var otherProcessGCHandle))
    //{
    //    window2GCHandle = new IntPtr(otherProcessGCHandle);
    //}
    // 不用别人传的，从窗口进行创建
    window2GCHandle = XCreateGC(display, window2Handle, 0, 0);
    Console.WriteLine($"XCreateGC Window2 {window2GCHandle}");
}



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
        if (args.Length == 0)
        {
            XDrawLine(display, window1.Window, window1.GC, 0, 0, 100, 100);
        }
    }
    else if (@event.type == XEventName.MotionNotify)
    {
        var x = @event.MotionEvent.x;
        var y = @event.MotionEvent.y;

        if (window2Handle != 0 && window2GCHandle != 0)
        {
            XDrawLine(display, window2Handle, window2GCHandle, x, y, x + 100, y);
        }

        //if (@event.MotionEvent.window == window1.Window)
        //{
        //    XDrawLine(display, window1.Window, window1.GC, x, y, x + 100, y);
        //}
        //else
        //{
        //    var xEvent = new XEvent
        //    {
        //        MotionEvent =
        //        {
        //            type = XEventName.MotionNotify,
        //            send_event = true,
        //            window = window1.Window,
        //            display = display,
        //            x = x,
        //            y = y
        //        }
        //    };
        //    XSendEvent(display, window1.Window, propagate: false, new IntPtr((int)(EventMask.ButtonMotionMask)),
        //        ref xEvent);
        //}
    }

    // 这是有用的
    //var count = XEventsQueued(display, 0 /*QueuedAlready*/);
    //if (count == 0)
    //{
    //    var result = XIconifyWindow(display, window1.Window, screen);
    //    Console.WriteLine($"XIconifyWindow {result}");
    //}
}

Console.WriteLine("Hello, World!");

class FooWindow
{
    public FooWindow(nint windowHandle, nint display)
    {
        Window = windowHandle;

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
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
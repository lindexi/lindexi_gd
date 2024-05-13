// See https://aka.ms/new-console-template for more information

using CPF.Linux;
using static CPF.Linux.XLib;

var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);

var white = XWhitePixel(display, screen);
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

var xDisplayWidth = XDisplayWidth(display, screen);
var xDisplayHeight = XDisplayHeight(display, screen);
var handle = XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes);

var window = handle;

XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
XSelectInput(display, window, mask);

XMapWindow(display, window);
XFlush(display);

var gc = XCreateGC(display, window, 0, 0);
XSetForeground(display, gc, white);

//XSetInputFocus(Display, Window, 0, IntPtr.Zero);

XSync(display, false);

Task.Run(() =>
{

});

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

        XDrawLine(display, @event.MotionEvent.window, gc, x, y, x + 100, y);
    }

    var count = XEventsQueued(display, 0 /*QueuedAlready*/);
    if (count == 0)
    {
        for (int i = 0; i < 100; i++)
        {
            var xEvent = new XEvent
            {
                MotionEvent =
                {
                    type = XEventName.MotionNotify,
                    send_event = true,
                    window = window,
                    display = display,
                    x = i,
                    y = i
                }
            };
            XSendEvent(display, window, propagate: false, new IntPtr((int) (EventMask.ButtonMotionMask)), ref xEvent);
        }
    }
}

Console.WriteLine("Hello, World!");
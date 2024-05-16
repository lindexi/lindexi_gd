// See https://aka.ms/new-console-template for more information

using CPF.Linux;
using static CPF.Linux.XLib;

var Display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(Display);
var Screen = screen;

var white = XWhitePixel(Display, screen);
var black = XBlackPixel(Display, screen);

var rootWindow = XDefaultRootWindow(Display);
var RootWindow = rootWindow;

XMatchVisualInfo(Display, screen, 32, 4, out var info);
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
    colormap = XCreateColormap(Display, rootWindow, visual, 0),
    border_pixel = 0,
    background_pixel = 0,
};

var xDisplayWidth = XDisplayWidth(Display, screen);
var xDisplayHeight = XDisplayHeight(Display, screen);
var handle = XCreateWindow(Display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes);

var window1 = handle;

XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
XSelectInput(Display, window1, mask);

XMapWindow(Display, window1);
XFlush(Display);

var GC = XCreateGC(Display, window1, 0, 0);
XSetForeground(Display, GC, white);

//XSetInputFocus(Display, Window, 0, IntPtr.Zero);

XSync(Display, false);

var window2 = XCreateWindow(Display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes);
XMapWindow(Display, window2);
XFlush(Display);
XSync(Display, false);


while (true)
{
    var xNextEvent = XNextEvent(Display, out var @event);
    if (@event.type == XEventName.MotionNotify)
    {
        var x = @event.MotionEvent.x;
        var y = @event.MotionEvent.y;

        XDrawLine(Display, window1, GC, x, y, x + 100, y);
    }

    var count = XEventsQueued(Display, 0 /*QueuedAlready*/);
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
                    window = window1,
                    display = Display,
                    x = i,
                    y = i
                }
            };
            XSendEvent(Display, window1, propagate: false, new IntPtr((int) (EventMask.ButtonMotionMask)), ref xEvent);
        }
    }
}

Console.WriteLine("Hello, World!");
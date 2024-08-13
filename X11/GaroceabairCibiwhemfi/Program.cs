using CPF.Linux;

using static CPF.Linux.XLib;

XInitThreads();
var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);
var rootWindow = XDefaultRootWindow(display);

XMatchVisualInfo(display, screen, depth: 32, klass: 4, out var info);
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

var width = xDisplayWidth / 2;
var height = xDisplayHeight / 2;

var handle = XCreateWindow(display, rootWindow, x: 0, y: 0, width, height, border_width: 5,
    depth: 32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes);

XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
XSelectInput(display, handle, mask);

XMapWindow(display, handle);
XFlush(display);

bool destroyWindow = false;

IntPtr invokeMessageId = new IntPtr(123123123);

Task.Run(() =>
{
    var newDisplay = XOpenDisplay(IntPtr.Zero);

    Console.ReadLine();
    var @event = new XEvent
    {
        ClientMessageEvent =
        {
            type = XEventName.ClientMessage,
            send_event = true,
            window = handle,
            message_type = 0,
            format = 32,
            ptr1 = invokeMessageId,
            ptr2 = 0,
            ptr3 = 0,
            ptr4 = 0,
        }
    };

    // 似乎如此发送是不安全的
    XLib.XSendEvent(newDisplay, handle, false, 0, ref @event);
    XLib.XFlush(newDisplay);
    Console.WriteLine($"发送");

    Console.ReadLine();
    Console.WriteLine($"发送");
    XLib.XSendEvent(newDisplay, handle, false, 0, ref @event);
    XLib.XFlush(newDisplay);
    XCloseDisplay(newDisplay);
});

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);

    if (xNextEvent != 0)
    {
        break;
    }

    if (@event.type == XEventName.ClientMessage)
    {
        if (@event.ClientMessageEvent.ptr1 == invokeMessageId)
        {
            if (!destroyWindow)
            {
                Console.WriteLine("删除窗口");
                XDestroyWindow(display, handle);
                XFlush(display);
                destroyWindow = true;
            }
            else
            {
                Console.WriteLine("映射窗口");
                XMapWindow(display, handle);
                XFlush(display);
            }
        }
    }
}
// See https://aka.ms/new-console-template for more information

using CPF.Linux;
using System.Runtime.InteropServices;

using static CPF.Linux.XLib;
using static CPF.Linux.ShapeConst;
using System.Threading;

//var manualResetEvent = new ManualResetEvent(false);
//IntPtr window1 = IntPtr.Zero;

//var thread1 = new Thread(() =>
//{
//    var display = XOpenDisplay(IntPtr.Zero);
//    var screen = XDefaultScreen(display);

//    var rootWindow = XDefaultRootWindow(display);

//    XMatchVisualInfo(display, screen, 32, 4, out var info);
//    var visual = info.visual;

//    var valueMask =
//            //SetWindowValuemask.BackPixmap
//            0
//            | SetWindowValuemask.BackPixel
//            | SetWindowValuemask.BorderPixel
//        //| SetWindowValuemask.BitGravity
//        //| SetWindowValuemask.WinGravity
//        //| SetWindowValuemask.BackingStore
//        //| SetWindowValuemask.ColorMap
//        //| SetWindowValuemask.OverrideRedirect
//        ;
//    var xSetWindowAttributes = new XSetWindowAttributes
//    {
//        backing_store = 1,
//        bit_gravity = Gravity.NorthWestGravity,
//        win_gravity = Gravity.NorthWestGravity,
//        //override_redirect = true, // 设置窗口的override_redirect属性为True，以避免窗口管理器的干预
//        colormap = XCreateColormap(display, rootWindow, visual, 0),
//        border_pixel = 0,
//        background_pixel = new IntPtr(0x65565656),
//    };

//    var width = 500;
//    var height = 500;
//    var handle = XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
//        0,
//        (int) CreateWindowArgs.CopyFromParent,
//        0,
//        (nuint) valueMask, ref xSetWindowAttributes);

//    XMapWindow(display, handle);

//    XFlush(display);

//    window1 = handle;
//    manualResetEvent.Reset();

//    while (true)
//    {
//        var xNextEvent = XNextEvent(display, out var @event);
//        if (xNextEvent != 0)
//        {
//            break;
//        }
//    }
//});
//thread1.Start();

//var thread2 = new Thread(() =>
//{
//    var display = XOpenDisplay(IntPtr.Zero);
//    var screen = XDefaultScreen(display);

//    var rootWindow = XDefaultRootWindow(display);

//    XMatchVisualInfo(display, screen, 32, 4, out var info);
//    var visual = info.visual;

//    var valueMask =
//            //SetWindowValuemask.BackPixmap
//            0
//            | SetWindowValuemask.BackPixel
//            | SetWindowValuemask.BorderPixel
//            | SetWindowValuemask.BitGravity
//            | SetWindowValuemask.WinGravity
//            | SetWindowValuemask.BackingStore
//            | SetWindowValuemask.ColorMap
//        //| SetWindowValuemask.OverrideRedirect
//        ;
//    var xSetWindowAttributes = new XSetWindowAttributes
//    {
//        backing_store = 1,
//        bit_gravity = Gravity.NorthWestGravity,
//        win_gravity = Gravity.NorthWestGravity,
//        //override_redirect = true, // 设置窗口的override_redirect属性为True，以避免窗口管理器的干预
//        colormap = XCreateColormap(display, rootWindow, visual, 0),
//        border_pixel = 0,
//        background_pixel = new IntPtr(0xAFA6A656),
//    };

//    var width = 500;
//    var height = 500;
//    var handle = XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
//        32,
//        (int) CreateWindowArgs.InputOutput,
//        visual,
//        (nuint) valueMask, ref xSetWindowAttributes);

//    // 设置不接受输入
//    SetClickThrough();

//    XSetTransientForHint(display, handle, window1);

//    XMapWindow(display, handle);

//    unsafe
//    {
//        var devices = (XIDeviceInfo*) XLib.XIQueryDevice(display,
//            (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);
//        Console.WriteLine($"不会卡住");
//    }

//    XFlush(display);

//    while (true)
//    {
//        var xNextEvent = XNextEvent(display, out var @event);
//        if (xNextEvent != 0)
//        {
//            break;
//        }
//    }

//    // 点击命中穿透
//    void SetClickThrough()
//    {
//        // 设置不接受输入
//        // 这样输入穿透到后面一层里，由后面一层将内容上报上来
//        var region = XCreateRegion();
//        XShapeCombineRegion(display, handle, ShapeInput, 0, 0, region, ShapeSet);
//    }
//});
//thread2.Start();

var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);

var rootWindow = XRootWindow(display, screen);
var rootWindowWindowAttributes = new XWindowAttributes();
XGetWindowAttributes(display, rootWindow, ref rootWindowWindowAttributes);
Console.WriteLine($"RootWindowDepth={rootWindowWindowAttributes.depth}");
return;

var valueMask =
        //SetWindowValuemask.BackPixmap
        0
        | SetWindowValuemask.BackPixel
        | SetWindowValuemask.BorderPixel
    //| SetWindowValuemask.BitGravity
    //| SetWindowValuemask.WinGravity
    //| SetWindowValuemask.BackingStore
    //| SetWindowValuemask.ColorMap
    //| SetWindowValuemask.OverrideRedirect
    ;
var xSetWindowAttributes = new XSetWindowAttributes
{
    //backing_store = 1,
    //bit_gravity = Gravity.NorthWestGravity,
    //win_gravity = Gravity.NorthWestGravity,
    //override_redirect = true, // 设置窗口的override_redirect属性为True，以避免窗口管理器的干预
    //colormap = XCreateColormap(display, rootWindow, visual, 0),
    border_pixel = 0,
    background_pixel = new IntPtr(0x65565656),
};

var width = 500;
var height = 500;
var handle = XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
    0,
    (int) CreateWindowArgs.CopyFromParent,
    0,
    (nuint) valueMask, ref xSetWindowAttributes);

var windowAttributes = new XWindowAttributes();
XGetWindowAttributes(display, handle, ref windowAttributes);
Console.WriteLine($"Depth={windowAttributes.depth}");

XMapWindow(display, handle);

XFlush(display);

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);
    if (xNextEvent != 0)
    {
        break;
    }
}


const string libX11 = "libX11.so.6";

[DllImport(libX11)]
static extern IntPtr XCreateRegion();

[DllImport("libXext.so.6")]
static extern void XShapeCombineRegion(IntPtr display, IntPtr dest, int destKind, int xOff, int yOff, IntPtr region, int op);
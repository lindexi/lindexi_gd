// See https://aka.ms/new-console-template for more information

using CPF.Linux;
<<<<<<< HEAD
<<<<<<< HEAD

using System.Drawing;
using System.Runtime.InteropServices;

=======
>>>>>>> f3dd07d4f86f5759a8b16ec2da038e7c6baa733b
=======
using System.Runtime.InteropServices;

>>>>>>> 8b3e3749a9c3dd35a52c101162d9d6ba4e7ced3b
using static CPF.Linux.XLib;
using static CPF.Linux.ShapeConst;

<<<<<<< HEAD
var depths = XListDepths(display,screen,out var countReturn);
unsafe
{
    int* depthPtr = (int*)depths;

    for (int i = 0; i < countReturn; i++)
    {
        var depthValue = *depthPtr;
        depthPtr++;
        Console.WriteLine($"Depth:{depthValue}");
    }
}

XFree(depths);

var rootWindow = XDefaultRootWindow(display);

var defaultDepth = XDefaultDepth(display, screen);
Console.WriteLine(defaultDepth); // 默认是 24 的值
/*
   int XDefaultDepth(Display *dpy, int scr)
   {
       return(DefaultDepth(dpy, scr));
   }

// Xlib.h
#define DefaultDepth(dpy, scr) 	(ScreenOfDisplay(dpy,scr)->root_depth)

// libx11\src\Macros.c
/* screen oriented macros (toolkit) * /
   Screen *XScreenOfDisplay(Display *dpy, int scr)
   {
       return (ScreenOfDisplay(dpy, scr));
   }

// libx11\include\X11\Xlib.h
#define ScreenOfDisplay(dpy, scr)(&((_XPrivDisplay)(dpy))->screens[scr])

typedef struct 
{
   	XExtData *ext_data;	/* hook for extension to hang data * /
   	struct _XDisplay *display;/* back pointer to display structure * /
   	Window root;		/* Root window id. * /
   	int width, height;	/* width and height of screen * /
   	int mwidth, mheight;	/* width and height of  in millimeters * /
   	int ndepths;		/* number of depths possible * /
   	Depth *depths;		/* list of allowable depths on the screen * /
   	int root_depth;		/* bits per pixel * /
   	Visual *root_visual;	/* root visual * /
   	GC default_gc;		/* GC for the root root visual * /
   	Colormap cmap;		/* default color map * /
   	unsigned long white_pixel;
   	unsigned long black_pixel;	/* White and Black pixel values * /
   	int max_maps, min_maps;	/* max and min color maps * /
   	int backing_store;	/* Never, WhenMapped, Always * /
   	Bool save_unders;
   	long root_input_mask;	/* initial root input mask * /
   } Screen;
也就是实际 XDefaultDepth 获取的是 Screen 结构体的 root_depth 的值
 */

var result = XMatchVisualInfo(display, screen, 128, 4, out var info);
var success = result != 0;
Console.WriteLine($"Result={result} info.depth={info.depth}");
if (!success)
{
    result = XMatchVisualInfo(display, screen, 32, 4, out info);
}

Console.WriteLine($"Result={result} info.depth={info.depth}");

foreach (var depth in (int[]) [1, 2, 4, 8, 12, 16, 24, 32])
{
    result = XMatchVisualInfo(display, screen, depth, 4, out info);
    Console.WriteLine($"Depth:{depth} Result:{result}");
}

var visual = info.visual;

<<<<<<< HEAD
XSetWindowAttributes attr = new XSetWindowAttributes();
var valueMask = default(SetWindowValuemask);
=======
var valueMask =
        //SetWindowValuemask.BackPixmap
        0
        | SetWindowValuemask.BackPixel
        | SetWindowValuemask.BorderPixel
        | SetWindowValuemask.BitGravity
        | SetWindowValuemask.WinGravity
        | SetWindowValuemask.BackingStore
        | SetWindowValuemask.ColorMap
    | SetWindowValuemask.OverrideRedirect
    ;
>>>>>>> 683370edc89f19b46c3ba42b809618d897b0eef2

attr.backing_store = 1;
attr.bit_gravity = Gravity.NorthWestGravity;
attr.win_gravity = Gravity.NorthWestGravity;
valueMask |= SetWindowValuemask.BackPixel | SetWindowValuemask.BorderPixel
                                          | SetWindowValuemask.BackPixmap | SetWindowValuemask.BackingStore
                                          | SetWindowValuemask.BitGravity | SetWindowValuemask.WinGravity;

var colormap = XCreateColormap(display, rootWindow, visual, 0);
attr.colormap = colormap;
valueMask |= SetWindowValuemask.ColorMap;

int defaultWidth = 0, defaultHeight = 0;
defaultWidth = Math.Max(defaultWidth, 300);
defaultHeight = Math.Max(defaultHeight, 200);


<<<<<<< HEAD
var handle = XCreateWindow(display, rootWindow, 10, 10, defaultWidth, defaultHeight, 0,
=======
Console.WriteLine(color.pixel.ToString("X"));

var xSetWindowAttributes = new XSetWindowAttributes
{
    backing_store = 1,
    bit_gravity = Gravity.NorthWestGravity,
    win_gravity = Gravity.NorthWestGravity,
    override_redirect = true, // 设置窗口的override_redirect属性为True，以避免窗口管理器的干预
    colormap = colormap,
    border_pixel = 0,
    background_pixel = color.pixel,
};

var width = 500;
var height = 500;
var handle = XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
>>>>>>> 683370edc89f19b46c3ba42b809618d897b0eef2
    (int) info.depth,
    (int) CreateWindowArgs.InputOutput,
    visual,
    new UIntPtr((uint) valueMask), ref attr);
=======
var manualResetEvent = new ManualResetEvent(false);
IntPtr window1 = IntPtr.Zero;

var thread1 = new Thread(() =>
{
    var display = XOpenDisplay(IntPtr.Zero);
    var screen = XDefaultScreen(display);

    var rootWindow = XDefaultRootWindow(display);
>>>>>>> f3dd07d4f86f5759a8b16ec2da038e7c6baa733b

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
        background_pixel = new IntPtr(0x65565656),
    };

<<<<<<< HEAD
Console.WriteLine("Hello, World!");

const string libX11 = "libX11.so.6";

[DllImport(libX11)]
static extern int XDefaultDepth(IntPtr display, int screen);


/*
int *XListDepths(display, screen_number, count_return)
   Display *display;
   int screen_number;
   int *count_return;
 */

[DllImport(libX11)]
static extern IntPtr XListDepths(IntPtr display, int screen, out int count_return);
=======
    var width = 500;
    var height = 500;
    var handle = XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
        32,
        (int)CreateWindowArgs.InputOutput,
        visual,
        (nuint)valueMask, ref xSetWindowAttributes);

    XMapWindow(display, handle);

    XFlush(display);

    window1 = handle;
    manualResetEvent.Reset();

    while (true)
    {
        var xNextEvent = XNextEvent(display, out var @event);
        if (xNextEvent != 0)
        {
            break;
        }
    }
});
thread1.Start();

var thread2 = new Thread(() =>
{
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
        background_pixel = new IntPtr(0xAFA6A656),
    };

    var width = 500;
    var height = 500;
    var handle = XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
        32,
        (int)CreateWindowArgs.InputOutput,
        visual,
        (nuint)valueMask, ref xSetWindowAttributes);

    // 设置不接受输入
    SetClickThrough();

    XSetTransientForHint(display, handle, window1);

    XMapWindow(display, handle);

    unsafe
    {
        var devices = (XIDeviceInfo*) XLib.XIQueryDevice(display,
            (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);
        Console.WriteLine($"不会卡住");
    }

    XFlush(display);

    while (true)
    {
        var xNextEvent = XNextEvent(display, out var @event);
        if (xNextEvent != 0)
        {
            break;
        }
    }

    // 点击命中穿透
    void SetClickThrough()
    {
        // 设置不接受输入
        // 这样输入穿透到后面一层里，由后面一层将内容上报上来
        var region = XCreateRegion();
        XShapeCombineRegion(display, handle, ShapeInput, 0, 0, region, ShapeSet);
    }
});
thread2.Start();
<<<<<<< HEAD
>>>>>>> f3dd07d4f86f5759a8b16ec2da038e7c6baa733b
=======




const string libX11 = "libX11.so.6";

[DllImport(libX11)]
static extern IntPtr XCreateRegion();

[DllImport("libXext.so.6")]
static extern void XShapeCombineRegion(IntPtr display, IntPtr dest, int destKind, int xOff, int yOff, IntPtr region, int op);
>>>>>>> 8b3e3749a9c3dd35a52c101162d9d6ba4e7ced3b

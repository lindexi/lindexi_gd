// See https://aka.ms/new-console-template for more information
using CPF.Linux;

using System.Drawing;
using System.Runtime.InteropServices;

using static CPF.Linux.XLib;
var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);

var rootWindow = XDefaultRootWindow(display);

Console.WriteLine(XDefaultDepth(display, screen)); // 默认是 24 的值
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

var result = XMatchVisualInfo(display, screen, 32, 4, out var info);
Console.WriteLine($"Result={result} info.depth={info.depth}");

var visual = info.visual;

XSetWindowAttributes attr = new XSetWindowAttributes();
var valueMask = default(SetWindowValuemask);

attr.backing_store = 1;
attr.bit_gravity = Gravity.NorthWestGravity;
attr.win_gravity = Gravity.NorthWestGravity;
valueMask |= SetWindowValuemask.BackPixel | SetWindowValuemask.BorderPixel
                                          | SetWindowValuemask.BackPixmap | SetWindowValuemask.BackingStore
                                          | SetWindowValuemask.BitGravity | SetWindowValuemask.WinGravity;

var depth = (int) info.depth;

var colormap = XCreateColormap(display, rootWindow, visual, 0);
attr.colormap = colormap;
valueMask |= SetWindowValuemask.ColorMap;

int defaultWidth = 0, defaultHeight = 0;
defaultWidth = Math.Max(defaultWidth, 300);
defaultHeight = Math.Max(defaultHeight, 200);


var handle = XCreateWindow(display, rootWindow, 10, 10, defaultWidth, defaultHeight, 0,
    depth,
    (int) CreateWindowArgs.InputOutput,
    visual,
    new UIntPtr((uint) valueMask), ref attr);

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

Console.WriteLine("Hello, World!");

const string libX11 = "libX11.so.6";

[DllImport(libX11)]
static extern int XDefaultDepth(IntPtr display, int screen);
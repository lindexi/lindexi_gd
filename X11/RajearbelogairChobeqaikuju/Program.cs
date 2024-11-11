using System.Diagnostics;
using CPF.Linux;

using SkiaSharp;

using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using static CPF.Linux.XLib;

XInitThreads();
var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);
var rootWindow = XDefaultRootWindow(display);

int major = 2, minor = 0;
XIQueryVersion(display, ref major, ref minor);

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

var width = xDisplayWidth;
var height = xDisplayHeight;

var handle = XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
visual,
(nuint) valueMask, ref xSetWindowAttributes);
XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
XSelectInput(display, handle, mask);

var pid = (uint) Environment.ProcessId;

var _NET_WM_PIDAtom = XInternAtom(display, "_NET_WM_PID", false);
IntPtr XA_CARDINAL = (IntPtr) 6;

XChangeProperty(display, handle,
    _NET_WM_PIDAtom, XA_CARDINAL, 32,
    PropertyMode.Replace, ref pid, 1);

var gc = XCreateGC(display, handle, 0, 0);
var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
var skCanvas = new SKCanvas(skBitmap);
var xImage = CreateImage(skBitmap);

Console.WriteLine($"WH={width},{height}");

skCanvas.Clear(SKColors.Blue);

using var skPaint = new SKPaint();
skPaint.Color = SKColors.Black;
skPaint.StrokeWidth = 2;
skPaint.Style = SKPaintStyle.Stroke;
skPaint.IsAntialias = true;

//for (int y = 0; y < skBitmap.Height; y += 25)
//{
//    skPaint.Color = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF);
//    skCanvas.DrawLine(0, y, skBitmap.Width, y, skPaint);
//}

//for (int x = 0; x < skBitmap.Width; x += 25)
//{
//    skPaint.Color = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF);
//    skCanvas.DrawLine(x, 0, x, skBitmap.Height, skPaint);
//}

//skCanvas.Flush();

//skCanvas.Clear(SKColors.White);

// 随意用一个支持中文的字体
var typeface = SKFontManager.Default.MatchCharacter('十');
skPaint.TextSize = 20;
skPaint.Typeface = typeface;
skPaint.Color = SKColors.Black;
//skCanvas.DrawText("中文", 100, 100, skPaint); // 测试绘制中文
skCanvas.Clear(SKColors.White);

var touchMajorAtom = XInternAtom(display, "Abs MT Touch Major", false);
var touchMinorAtom = XInternAtom(display, "Abs MT Touch Minor", false);
var pressureAtom = XInternAtom(display, "Abs MT Pressure", false);

Console.WriteLine($"ABS_MT_TOUCH_MAJOR={touchMajorAtom} Name={XLib.GetAtomName(display, touchMajorAtom)} ABS_MT_TOUCH_MINOR={touchMinorAtom} Name={XLib.GetAtomName(display, touchMinorAtom)} Abs_MT_Pressure={pressureAtom} Name={XLib.GetAtomName(display, pressureAtom)}");


// 先获取触摸再显示窗口，依然拿不到触摸宽度高度
XMapWindow(display, handle);
XFlush(display);

Action? action = null;

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);

    if (xNextEvent != 0)
    {
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        // _NET_WM_PID CARDINAL/32
        // https://specifications.freedesktop.org/wm-spec/1.3/ar01s05.html
        XGetWindowProperty(display, handle, _NET_WM_PIDAtom,
            IntPtr.Zero, new IntPtr(0x7fffffff),
            false, XA_CARDINAL, out var actualType, out var actualFormat,
            out var nitems, out _, out var prop);
        unsafe
        {
            var pidProperty = * (uint*) prop;
            Console.WriteLine($"PID={Environment.ProcessId:X}; Property={pidProperty:X} nitems={nitems.ToInt32()}");
        }

        XPutImage(display, handle, gc, ref xImage, @event.ExposeEvent.x, @event.ExposeEvent.y, @event.ExposeEvent.x, @event.ExposeEvent.y, (uint) @event.ExposeEvent.width,
            (uint) @event.ExposeEvent.height);
    }
    else if (@event.type == XEventName.MotionNotify)
    {
        var x = @event.MotionEvent.x;
        var y = @event.MotionEvent.y;

        action?.Invoke();
        action = null;

        skCanvas.Clear(new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF));
        skCanvas.Flush();

        SendExposeEvent(display, handle, 0, 0, width, height);
    }
    else if (@event.type is XEventName.PropertyNotify or XEventName.EnterNotify or XEventName.KeymapNotify or XEventName.LeaveNotify)
    {

    }
    else
    {
        Console.WriteLine($"Event={@event.type}");
    }
}

static XImage CreateImage(SKBitmap skBitmap)
{
    const int bytePerPixelCount = 4; // RGBA 一共4个 byte 长度
    var bitPerByte = 8;

    var bitmapWidth = skBitmap.Width;
    var bitmapHeight = skBitmap.Height;

    var img = new XImage();
    int bitsPerPixel = bytePerPixelCount * bitPerByte;
    img.width = bitmapWidth;
    img.height = bitmapHeight;
    img.format = 2; //ZPixmap;
    img.data = skBitmap.GetPixels();
    img.byte_order = 0; // LSBFirst;
    img.bitmap_unit = bitsPerPixel;
    img.bitmap_bit_order = 0; // LSBFirst;
    img.bitmap_pad = bitsPerPixel;
    img.depth = bitsPerPixel;
    img.bytes_per_line = bitmapWidth * bytePerPixelCount;
    img.bits_per_pixel = bitsPerPixel;
    XInitImage(ref img);

    return img;
}

static void SendExposeEvent(IntPtr display, IntPtr window, int x, int y, int width, int height)
{
    var exposeEvent = new XExposeEvent
    {
        type = XEventName.Expose,
        display = display,
        window = window,
        x = x,
        y = y,
        width = width,
        height = height,
        count = 1,
    };

    var xEvent = new XEvent
    {
        ExposeEvent = exposeEvent
    };

    XSendEvent(display, window, false, new IntPtr((int) (EventMask.ExposureMask)), ref xEvent);
    XFlush(display);
}

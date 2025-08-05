using BujeeberehemnaNurgacolarje;

using CPF.Linux;

using SkiaSharp;

using System.Diagnostics;
using System.Runtime.InteropServices;

using static CPF.Linux.XLib;

XInitThreads();

var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);
var rootWindow = XDefaultRootWindow(display);

var randr15ScreensImpl = new Randr15ScreensImpl(display, rootWindow);
var monitorInfos = randr15ScreensImpl.GetMonitorInfos();
for (var i = 0; i < monitorInfos.Length; i++)
{
    Console.WriteLine($"屏幕{i} {monitorInfos[i]}");
}

var xDisplayWidth = XDisplayWidth(display, screen);
var xDisplayHeight = XDisplayHeight(display, screen);

Console.WriteLine($"XDisplayWidth={xDisplayWidth}");
Console.WriteLine($"XDisplayHeight={xDisplayHeight}");

var x = 0;
var y = 0;

var width = xDisplayWidth;
var height = xDisplayHeight / 2;

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

var handle = XCreateWindow(display, rootWindow, x, y, width, height, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes);

Console.WriteLine($"Window={handle}");

XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
XSelectInput(display, handle, mask);

var gc = XCreateGC(display, handle, 0, 0);

XMapWindow(display, handle);

using var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

using var skCanvas = new SKCanvas(skBitmap);

var xImage = CreateImage(skBitmap);

skCanvas.Clear(new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF));
skCanvas.Flush();

XPutImage(display, handle, gc, ref xImage, 0, 0, 0, 0, (uint) skBitmap.Width,
    (uint) skBitmap.Height);

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);

    if (xNextEvent != 0)
    {
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        var window = @event.ExposeEvent.window;
        Debug.Assert(window == handle);

        skCanvas.Clear(new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF));
        skCanvas.Flush();

        XPutImage(display, handle, gc, ref xImage, 0, 0, 0, 0, (uint) skBitmap.Width,
            (uint) skBitmap.Height);
    }
    else if (@event.type == XEventName.PropertyNotify)
    {
        var atom = @event.PropertyEvent.atom;
        var atomNamePtr = XGetAtomName(display, atom);
        var atomName = Marshal.PtrToStringAnsi(atomNamePtr);
        XFree(atomNamePtr);
        //Console.WriteLine($"PropertyNotify {atomName}({atom}) State={@event.PropertyEvent.state}");
    }
    else if (@event.type is XEventName.KeymapNotify)
    {
        // 忽略
    }
    else if (@event.type is XEventName.ConfigureNotify)
    {
        Console.WriteLine($"ConfigureNotify {@event}");
    }
    else
    {
        //Console.WriteLine($"Event={@event}");
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
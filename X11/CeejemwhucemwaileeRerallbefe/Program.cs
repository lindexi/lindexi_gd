using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using CPF.Linux;

using SkiaSharp;

using static CPF.Linux.XLib;

XInitThreads();
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
    background_pixel = 0,
};

var xDisplayWidth = XDisplayWidth(display, screen);
var xDisplayHeight = XDisplayHeight(display, screen);

Console.WriteLine($"XDisplayWidth={xDisplayWidth}");
Console.WriteLine($"XDisplayHeight={xDisplayHeight}");

var width = xDisplayWidth /2;
var height = xDisplayHeight/2;

var handle = XCreateWindow(display, rootWindow, -290, 0, width, height, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes);

XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
XSelectInput(display, handle, mask);

XMapWindow(display, handle);
XFlush(display);

var gc = XCreateGC(display, handle, 0, 0);

var mapLength = width * 4 * height;
Console.WriteLine($"Length = {mapLength}");

var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

var skCanvas = new SKCanvas(skBitmap);

var xImage = CreateImage(skBitmap);



IntPtr mapCache = IntPtr.Zero;

//{
//    var stopwatch = Stopwatch.StartNew();
//    var length = 1000;
//    for (var i = 0; i < length; i++)
//    {
//        ReplacePixels(skBitmap2, skBitmap);
//    }
//    stopwatch.Stop();
//    Console.WriteLine($"拷贝耗时：{stopwatch.ElapsedMilliseconds * 1.0 / length}");
//}

skCanvas.Clear(new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF));
skCanvas.Flush();
XPutImage(display, handle, gc, ref xImage, 0, 0, 0, 0, (uint) skBitmap.Width,
    (uint) skBitmap.Height);

IntPtr invokeMessageId = new IntPtr(123123123);

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);

    if (xNextEvent != 0)
    {
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        skCanvas.Clear(new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF));
        skCanvas.Flush();

        XPutImage(display, handle, gc, ref xImage, 0, 0, 0, 0, (uint) skBitmap.Width,
            (uint) skBitmap.Height);
    }
    else if (@event.type == XEventName.ClientMessage)
    {
        if (@event.ClientMessageEvent.ptr1 == invokeMessageId)
        {
            skCanvas.Clear(new SKColor((uint) Random.Shared.Next()));
            skCanvas.Flush();

            var stopwatch = Stopwatch.StartNew();
            XPutImage(display, handle, gc, ref xImage, 0, 0, 0, 0, (uint) skBitmap.Width,
                (uint) skBitmap.Height);
            stopwatch.Stop();
            Console.WriteLine($"耗时：{stopwatch.ElapsedMilliseconds}");
        }
    }
}

[DllImport("libc", SetLastError = true)]
static extern IntPtr mmap(IntPtr addr, IntPtr length, int prot, int flags, int fd, IntPtr offset);
[DllImport("libc", SetLastError = true)]
static extern int munmap(IntPtr addr, IntPtr length);



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

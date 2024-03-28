using System.Runtime.Loader;
using static CPF.Linux.XLib;
using CPF.Linux;
using System.Runtime.InteropServices;

XInitThreads();
var display = XOpenDisplay(IntPtr.Zero);
XError.Init();

var screen = XDefaultScreen(display);

var rootWindow = XDefaultRootWindow(display);

XMatchVisualInfo(display, screen, 32, 4, out var info);
var visual = info.visual;

var valueMask = SetWindowValuemask.BackPixmap
                | SetWindowValuemask.BackPixel
                | SetWindowValuemask.BorderPixel
                | SetWindowValuemask.BitGravity
                | SetWindowValuemask.WinGravity
                | SetWindowValuemask.BackingStore
                | SetWindowValuemask.ColorMap;
var attr = new XSetWindowAttributes
{
    backing_store = 1,
    bit_gravity = Gravity.NorthWestGravity,
    win_gravity = Gravity.NorthWestGravity,
    override_redirect = false,  // 参数：_overrideRedirect
    colormap = XCreateColormap(display, rootWindow, visual, 0),
};

var handle = XCreateWindow(display, rootWindow, 100, 100, 500, 500, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref attr);

var window = handle;

XSelectInput(display, window, new IntPtr((int) XEventMask.ExposureMask));

XMapWindow(display, window);
XFlush(display);

var gc = XCreateGC(display, window, 0, 0);

while (XNextEvent(display, out var xEvent) == default)
{
    if (xEvent.type == XEventName.Expose)
    {
        XImage img = CreateImage();
        XPutImage(display, window, gc, ref img, 0, 0, Random.Shared.Next(100), Random.Shared.Next(100), (uint) img.width, (uint) img.height);
    }
}

unsafe XImage CreateImage()
{
    var bitmapWidth = 50;
    var bitmapHeight = 50;

    const int bytePerPixelCount = 4; // RGBA 一共4个 byte 长度
    var bitPerByte = 8;

    var bitmapData = new byte[bitmapWidth * bitmapHeight * bytePerPixelCount];

    fixed (byte* p = bitmapData)
    {
        int* pInt = (int*) p;
        var color = Random.Shared.Next();
        for (var i = 0; i < bitmapData.Length / (sizeof(int) / sizeof(byte)); i++)
        {
            *(pInt + i) = color;
        }
    }

    GCHandle pinnedArray = GCHandle.Alloc(bitmapData, GCHandleType.Pinned);

    var img = new XImage();
    int bitsPerPixel = bytePerPixelCount * bitPerByte;
    img.width = bitmapWidth;
    img.height = bitmapHeight;
    img.format = 2; //ZPixmap;
    img.data = pinnedArray.AddrOfPinnedObject();
    img.byte_order = 0;// LSBFirst;
    img.bitmap_unit = bitsPerPixel;
    img.bitmap_bit_order = 0;// LSBFirst;
    img.bitmap_pad = bitsPerPixel;
    img.depth = bitsPerPixel;
    img.bytes_per_line = bitmapWidth * bytePerPixelCount;
    img.bits_per_pixel = bitsPerPixel;
    XInitImage(ref img);

    // 除非 XImage 不再使用了，否则此时释放，将会导致 GC 之后 data 指针对应的内存不是可用的
    // 调用 XPutImage 将访问不可用内存，导致段错误，闪退
    //pinnedArray.Free();

    return img;
}

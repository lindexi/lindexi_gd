using CPF.Linux;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static CPF.Linux.XLib;

namespace DayjayjuyallLaijojijereqaw;

internal class TestX11Window
{
    public TestX11Window(int x, int y, int width, int height, nint display,nint rootWindow,int screen)
    {
        Display = display;

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

        X11Window = handle;

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XSelectInput(display, handle, mask);

        var gc = XCreateGC(display, handle, 0, 0);
        GC = gc;

        Width = width;
        Height = height;
    }

    public IntPtr X11Window { get; }

    public IntPtr Display { get; }
    public IntPtr GC { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public void MapWindow()
    {
        XMapWindow(Display, X11Window);
        XFlush(Display);
    }

    public void Draw()
    {
        using var skBitmap = new SKBitmap(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using var skCanvas = new SKCanvas(skBitmap);

        var xImage = CreateImage(skBitmap);

        skCanvas.Clear(new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF));
        skCanvas.Flush();

        XPutImage(Display, X11Window, GC, ref xImage, 0, 0, 0, 0, (uint) skBitmap.Width,
            (uint) skBitmap.Height);
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


}

using System.Runtime.Loader;
using static CPF.Linux.XLib;
using CPF.Linux;
using System.Collections;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace BujeeberehemnaNurgacolarje;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        //var skBitmap = new SKBitmap(50, 50)
        //{

        //};
        //Console.WriteLine(skBitmap.ColorType); // BGRA 格式

        AssemblyLoadContext.Default.Resolving += Default_Resolving;

        StartX11App();
    }

    private static System.Reflection.Assembly? Default_Resolving(AssemblyLoadContext context,
        System.Reflection.AssemblyName assemblyName)
    {
        var file = $"{assemblyName.Name}.dll";
        file = Path.Join(AppContext.BaseDirectory, file);

        if (File.Exists(file))
        {
            return context.LoadFromAssemblyPath(file);
        }

        return null;
    }

    private static void StartX11App()
    {
        var app = new App();
        app.Run();
    }
}

class App
{
    public App()
    {
        XInitThreads();
        Display = XOpenDisplay(IntPtr.Zero);
        XError.Init();

        Info = new X11Info(Display, DeferredDisplay);
        Console.WriteLine("XInputVersion=" + Info.XInputVersion);
        var screen = XDefaultScreen(Display);
        Console.WriteLine($"Screen = {screen}");
        Screen = screen;
        var white = XWhitePixel(Display, screen);
        var black = XBlackPixel(Display, screen);

        var rootWindow = XDefaultRootWindow(Display);

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
            | SetWindowValuemask.ColorMap;
        var attr = new XSetWindowAttributes
        {
            backing_store = 1,
            bit_gravity = Gravity.NorthWestGravity,
            win_gravity = Gravity.NorthWestGravity,
            override_redirect = false, // 参数：_overrideRedirect
            colormap = XCreateColormap(Display, rootWindow, visual, 0),
        };

        var handle = XCreateWindow(Display, rootWindow, 100, 100, 1000, 500, 5,
            32,
            (int) CreateWindowArgs.InputOutput,
            visual,
            (nuint) valueMask, ref attr);

        Window = handle;

        //Window = XCreateSimpleWindow(Display, rootWindow, 0, 0, 500, 300, 5, white, black);

        Console.WriteLine($"Window={Window}");

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XSelectInput(Display, Window, mask);

        XMapWindow(Display, Window);
        XFlush(Info.Display);

        GC = XCreateGC(Display, Window, 0, 0);
        XSetForeground(Display, GC, white);

        Console.WriteLine($"App");

        var skBitmap = new SKBitmap(500, 500);
        _skBitmap = skBitmap;

        XImage img = CreateImage();
        _image = img;
    }

    private XImage _image;

    public void Run()
    {
        XSetInputFocus(Display, Window, 0, IntPtr.Zero);

        while (true)
        {
            XSync(Display, false);

            var xNextEvent = XNextEvent(Display, out var @event);
            //Console.WriteLine($"NextEvent={xNextEvent} {@event}");

            if (@event.type == XEventName.Expose)
            {
                //// 曝光时，可以收到需要重新绘制的范围
                //Console.WriteLine(
                //    $"Expose X={@event.ExposeEvent.x} Y={@event.ExposeEvent.y} W={@event.ExposeEvent.width} H={@event.ExposeEvent.height} CurrentWindow={@event.ExposeEvent.window == Window}");

                XPutImage(Display, Window, GC, ref _image, @event.ExposeEvent.x, @event.ExposeEvent.y, @event.ExposeEvent.x, @event.ExposeEvent.y, (uint) @event.ExposeEvent.width,
                    (uint) @event.ExposeEvent.height);

                Redraw();
            }
            else if (@event.type == XEventName.ButtonPress)
            {
                _lastPoint = (@event.ButtonEvent.x, @event.ButtonEvent.y);
                _isDown = true;
            }
            else if (@event.type == XEventName.MotionNotify)
            {
                if (_isDown)
                {
                    var x = @event.MotionEvent.x;
                    var y = @event.MotionEvent.y;

                    if (x < 500)
                    {
                        var additionSize = 10;
                        var minX = Math.Min(x, _lastPoint.X) - additionSize;
                        var minY = Math.Min(y, _lastPoint.Y) - additionSize;
                        var width = Math.Abs(x - _lastPoint.X) + additionSize * 2;
                        var height = Math.Abs(y - _lastPoint.Y) + additionSize * 2;

                        minX = Math.Max(0, minX);
                        minY = Math.Max(0, minY);

                        if (minX + width > _image.width)
                        {
                            width = _image.width - minX;
                        }

                        if (minY + height > _image.height)
                        {
                            height = _image.height - minY;
                        }

                        // 测试在按下时配置曝光尺寸
                        var xev = new XEvent
                        {
                            ExposeEvent =
                            {
                                type = XEventName.Expose,
                                send_event = true,
                                window = Window,
                                count = 1,
                                display = Display,
                                height = height,
                                width = width,
                                x = minX,
                                y = minY
                            }
                        };
                        // [Xlib Programming Manual: Expose Events](https://tronche.com/gui/x/xlib/events/exposure/expose.html )
                        XSendEvent(Display, Window, propagate: false, new IntPtr((int) (EventMask.ExposureMask)), ref xev);

                        _skBitmap.NotifyPixelsChanged();

                        using var skCanvas = new SKCanvas(_skBitmap);
                        //skCanvas.Clear(SKColors.Transparent);
                        //skCanvas.Translate(-minX,-minY);
                        using var skPaint = new SKPaint();
                        skPaint.StrokeWidth = 5;
                        skPaint.Color = SKColors.Red;
                        skPaint.IsAntialias = true;
                        skPaint.Style = SKPaintStyle.Fill;
                        skCanvas.DrawLine(_lastPoint.X, _lastPoint.Y, x, y, skPaint);
                        skCanvas.Flush();

                        var bitmapWidth = _skBitmap.Width;
                        var bitmapHeight = _skBitmap.Height;
                        //var bitmapWidth = 50;
                        //var bitmapHeight = 50;

                        var centerX = x - bitmapWidth / 2;
                        var centerY = y - bitmapHeight / 2;

                        //XPutImage(Display, Window, GC, ref _image, minX, minY, minX, minY, (uint) width,
                        //    (uint) height);
                    }
                    else
                    {
                        XDrawLine(Display, Window, GC, _lastPoint.X, _lastPoint.Y, x, y);
                    }

                    _lastPoint = (x, y);
                }
            }
            else if (@event.type == XEventName.ButtonRelease)
            {
                _isDown = false;
            }

            if (xNextEvent != 0)
            {
                break;
            }
        }
    }

    private (int X, int Y) _lastPoint;
    private bool _isDown;
    private SKBitmap _skBitmap;

    private void Redraw()
    {
        //var img = _image;

        //XPutImage(Display, Window, GC, ref img, 0, 0, Random.Shared.Next(100), Random.Shared.Next(100), (uint)img.width,
        //    (uint)img.height);
    }

    private unsafe XImage CreateImage()
    {
        //var bitmapWidth = 50;
        //var bitmapHeight = 50;

        const int bytePerPixelCount = 4; // RGBA 一共4个 byte 长度
        var bitPerByte = 8;

        //var bitmapData = new byte[bitmapWidth * bitmapHeight * bytePerPixelCount];

        //fixed (byte* p = bitmapData)
        //{
        //    int* pInt = (int*) p;
        //    var color = Random.Shared.Next();
        //    for (var i = 0; i < bitmapData.Length / (sizeof(int) / sizeof(byte)); i++)
        //    {
        //        *(pInt + i) = color;
        //    }
        //}
        //GCHandle pinnedArray = GCHandle.Alloc(bitmapData, GCHandleType.Pinned);

        var bitmapWidth = _skBitmap.Width;
        var bitmapHeight = _skBitmap.Height;

        var img = new XImage();
        int bitsPerPixel = bytePerPixelCount * bitPerByte;
        img.width = bitmapWidth;
        img.height = bitmapHeight;
        img.format = 2; //ZPixmap;
        //img.data = pinnedArray.AddrOfPinnedObject();
        img.data = _skBitmap.GetPixels();
        img.byte_order = 0; // LSBFirst;
        img.bitmap_unit = bitsPerPixel;
        img.bitmap_bit_order = 0; // LSBFirst;
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

    private IntPtr GC { get; }

    public IntPtr DeferredDisplay { get; set; }
    public IntPtr Display { get; set; }

    //public XI2Manager XI2;
    public X11Info Info { get; private set; }
    public IntPtr Window { get; set; }
    public int Screen { get; set; }
}
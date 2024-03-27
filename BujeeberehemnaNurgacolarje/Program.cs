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
            override_redirect = false,  // 参数：_overrideRedirect
            colormap = XCreateColormap(Display, rootWindow, visual, 0),
        };

        var handle = XCreateWindow(Display, rootWindow, 100, 100, 500, 500, 5,
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
                    var cx = @event.MotionEvent.x - _image.width / 2;
                    var cy = @event.MotionEvent.y - _image.height / 2;
                    if (_bitmapData != null)
                    {
                        var r = (byte) (cx ^ cy + Random.Shared.Next(byte.MaxValue));
                        var g = (byte) (cx ^ cy + Random.Shared.Next(byte.MaxValue));
                        var b = (byte) (cx ^ cy + Random.Shared.Next(byte.MaxValue));
                        var a = (byte) (cx ^ cy + Random.Shared.Next(byte.MaxValue));

                        for (var i = 0; i < _bitmapData.Length; i += 4)
                        {
                            _bitmapData[i] = r;
                            _bitmapData[i + 1] = g;
                            _bitmapData[i + 2] = b;
                            _bitmapData[i + 3] = a;
                        }
                    }

                    XPutImage(Display, Window, GC, ref _image, 0, 0, cx,
                        cy, (uint) _image.width, (uint) _image.height);
                    //XDrawLine(Display, Window, GC, _lastPoint.X, _lastPoint.Y, @event.MotionEvent.x,
                    //    @event.MotionEvent.y);
                    _lastPoint = (@event.MotionEvent.x, @event.MotionEvent.y);
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

    private void Redraw()
    {
        var img = _image;

        XPutImage(Display, Window, GC, ref img, 0, 0, 0, 0, (uint) img.width, (uint) img.height);
    }

    private byte[]? _bitmapData;

    private XImage CreateImage()
    {
        var bitmapWidth = 50;
        var bitmapHeight = 50;

        var perPixelByteCount = 4;

        var bitmapData = new byte[bitmapWidth * bitmapHeight * perPixelByteCount];
        _bitmapData = bitmapData;

        Random.Shared.NextBytes(bitmapData);

        GCHandle pinnedArray = GCHandle.Alloc(bitmapData, GCHandleType.Pinned);

        var img = new XImage();
        int bitsPerPixel = 32;
        img.width = bitmapWidth;
        img.height = bitmapHeight;
        img.format = 2; //ZPixmap;
        img.data = pinnedArray.AddrOfPinnedObject();
        img.byte_order = 0;// LSBFirst;
        img.bitmap_unit = bitsPerPixel;
        img.bitmap_bit_order = 0;// LSBFirst;
        img.bitmap_pad = bitsPerPixel;
        img.depth = 32;
        img.bytes_per_line = bitmapWidth * 4;
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


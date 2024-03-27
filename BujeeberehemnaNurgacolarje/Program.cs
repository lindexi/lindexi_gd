using System.Runtime.Loader;
using static CPF.Linux.XLib;
using CPF.Linux;

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
        Window = XCreateSimpleWindow(Display, XDefaultRootWindow(Display), 0, 0, 500, 300, 5, white, black);

        Console.WriteLine($"Window={Window}");

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XSelectInput(Display, Window, mask);

        XMapWindow(Display, Window);

        //XFlush(Info.Display);
        GC = XCreateGC(Display, Window, 0, 0);

        XSetForeground(Display, GC, white);
    }

    public unsafe void Run()
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

                    XDrawLine(Display, Window, GC, _lastPoint.X, _lastPoint.Y, @event.MotionEvent.x,
                        @event.MotionEvent.y);
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

    private unsafe void Redraw()
    {
        var perPixelByteCount = 4;
        var bitmapWidth = 10;
        var bitmapHeight = 10;

        var bitmapData = new byte[bitmapWidth * bitmapHeight * perPixelByteCount * 10];
        for (var i = 0; i < bitmapData.Length; i++)
        {
            bitmapData[i] = 100;
        }

        fixed (byte* p = bitmapData)
        {
            var img = new XImage();
            int bitsPerPixel = 32;
            img.width = bitmapWidth;
            img.height = bitmapHeight;
            img.format = 2; //ZPixmap;

            img.byte_order = 0;// LSBFirst;
            img.bitmap_unit = bitsPerPixel;
            img.bitmap_bit_order = 0;// LSBFirst;
            img.bitmap_pad = bitsPerPixel;
            img.depth = bitsPerPixel;
            img.bytes_per_line = bitmapWidth * perPixelByteCount;
            img.bits_per_pixel = bitsPerPixel;
            img.data = new IntPtr(p);

            var result = XInitImage(ref img);
            Console.WriteLine($"XInitImage={result}");
            result = XPutImage(Display, Window, GC, ref img, 0, 0, 10, 10, (uint) bitmapWidth, (uint) bitmapHeight);
            Console.WriteLine($"XPutImage={result}");
        }
    }

    private IntPtr GC { get; }

    public IntPtr DeferredDisplay { get; set; }
    public IntPtr Display { get; set; }

    //public XI2Manager XI2;
    public X11Info Info { get; private set; }
    public IntPtr Window { get; set; }
    public int Screen { get; set; }
}
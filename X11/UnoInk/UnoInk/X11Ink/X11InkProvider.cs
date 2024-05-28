using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;
using Windows.Foundation;
using CPF.Linux;
using ReewheaberekaiNayweelehe;
using SkiaSharp;
using static CPF.Linux.XFixes;
using static CPF.Linux.XLib;
using static CPF.Linux.ShapeConst;

namespace UnoInk.X11Ink;

//interface IX11WindowManager

[SupportedOSPlatform("Linux")]
internal class X11InkProvider
{
    public X11InkProvider()
    {
        // 不需要调用了，因为在 UNO 底层已经调用
        //// 这句话不能调用多次 虽然调用多次不会炸
        // https://tronche.com/gui/x/xlib/display/XInitThreads.html
        // It is only necessary to call this function if multiple threads might use Xlib concurrently. If all calls to Xlib functions are protected by some other access mechanism (for example, a mutual exclusion lock in a toolkit or through explicit client programming), Xlib thread initialization is not required. It is recommended that single-threaded programs not call this function.
        //XInitThreads();
        //XInitThreads();
        //XInitThreads();
        var display = XOpenDisplay(IntPtr.Zero);
        var screen = XDefaultScreen(display);

        if (XCompositeQueryExtension(display, out var eventBase, out var errorBase) == 0)
        {
            Console.WriteLine("Error: Composite extension is not supported");
            XCloseDisplay(display);
            throw new NotSupportedException("Error: Composite extension is not supported");
            return;
        }
        else
        {
            //Console.WriteLine("XCompositeQueryExtension");
        }

        var rootWindow = XDefaultRootWindow(display);

        var x11Info = new X11Info(display, screen, rootWindow);
        X11Info = x11Info;
    }

    public X11Info X11Info { get; }

    [MemberNotNull(nameof(_x11InkWindow))]
    public void Start(Window unoWindow)
    {
        var type = unoWindow.GetType();
        var nativeWindowPropertyInfo = type.GetProperty("NativeWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        var x11Window = nativeWindowPropertyInfo!.GetMethod!.Invoke(unoWindow, null)!;
        // Uno.WinUI.Runtime.Skia.X11.X11Window
        var x11WindowType = x11Window.GetType();

        var x11WindowIntPtr = (IntPtr) x11WindowType.GetProperty("Window", BindingFlags.Instance | BindingFlags.Public)!.GetMethod!.Invoke(x11Window, null)!;
        
        Console.WriteLine($"Uno 窗口句柄 {x11WindowIntPtr}");
        
        if (X11PlatformThreading == null)
        {
            X11PlatformThreading = new X11PlatformThreading(this);
            X11PlatformThreading.Run();
        }

        var x11InkWindow = new X11InkWindow(X11Info, x11WindowIntPtr, X11PlatformThreading);
        _x11InkWindow = x11InkWindow;
    }

    private X11PlatformThreading? X11PlatformThreading { get; set; }
    
    public X11InkWindow InkWindow
    {
        get
        {
            EnsureStart();
            return _x11InkWindow;
        }
    }
    
    private X11InkWindow? _x11InkWindow;

    [MemberNotNull(nameof(_x11InkWindow))]
    private void EnsureStart()
    {
        if (_x11InkWindow is null)
        {
            throw new InvalidOperationException();
        }
    }
}

[SupportedOSPlatform("Linux")]
class X11InkWindow
{
    public X11InkWindow(X11Info x11Info, IntPtr mainWindowHandle, X11PlatformThreading x11PlatformThreading)
    {
        X11PlatformThreading = x11PlatformThreading;
        _x11Info = x11Info;
        _mainWindowHandle = mainWindowHandle;
        var display = x11Info.Display;
        var rootWindow = x11Info.RootWindow;
        var screen = x11Info.Screen;

        var xDisplayWidth = XDisplayWidth(display, screen);
        var xDisplayHeight = XDisplayHeight(display, screen);

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
            background_pixel = IntPtr.Zero,
        };

        var childWindowHandle = XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
            32,
            (int) CreateWindowArgs.InputOutput,
            visual,
            (nuint) valueMask, ref xSetWindowAttributes);
        
        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XSelectInput(display, childWindowHandle, mask);

        // 设置不接受输入
        // 这样输入穿透到后面一层里，由后面一层将内容上报上来
        var region = XCreateRegion();
        XShapeCombineRegion(display, childWindowHandle, ShapeInput, 0, 0, region, ShapeSet); 


        XSetTransientForHint(display, childWindowHandle, mainWindowHandle);

        XMapWindow(display, childWindowHandle);
        
        GC = XCreateGC(display, childWindowHandle, 0, 0);

        X11InkWindowIntPtr = childWindowHandle;
        
        var skBitmap = new SKBitmap(xDisplayWidth, xDisplayHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        _skBitmap = skBitmap;
        var skCanvas = new SKCanvas(skBitmap);
        _skCanvas = skCanvas;
        
        XImage image = CreateImage();
        _image = image;

        // 读取屏幕物理尺寸，用于实现橡皮擦功能
        //UpdateScreenPhysicalSize();
        
        var skInkCanvas = // new SkInkCanvas(_skCanvas, _skBitmap);
            new SkInkCanvas();
        skInkCanvas.ApplicationDrawingSkBitmap = _skBitmap;
        skInkCanvas.SetCanvas(_skCanvas);
        
        skInkCanvas.Settings = skInkCanvas.Settings with
        {
            AutoSoftPen = false,
            //EnableEraserGesture = false,
        };
        
        skInkCanvas.RenderBoundsChanged += (sender, rect) =>
        {
            //if (PutImageBeforeExposeOnRenderBoundsChanged)
            //{
            //    var x = (int) rect.X;
            //    var y = (int) rect.Y;
            //    var width = (int) rect.Width;
            //    var height = (int) rect.Height;
                
            //    // 曝光之前推送图片
            //    XPutImage(Display, Window, GC, ref _image, x, y, x, y, (uint) width,
            //        (uint) height);
            //}
            
            var xEvent = new XEvent
            {
                ExposeEvent =
                {
                    type = XEventName.Expose,
                    send_event = true,
                    window = X11InkWindowIntPtr,
                    count = 1,
                    display = x11Info.Display,
                    height = (int)rect.Height,
                    width = (int)rect.Width,
                    x = (int)rect.X,
                    y = (int)rect.Y
                }
            };
            // [Xlib Programming Manual: Expose Events](https://tronche.com/gui/x/xlib/events/exposure/expose.html )
            XSendEvent(x11Info.Display, X11InkWindowIntPtr, propagate: false, new IntPtr((int) (EventMask.ExposureMask)), ref xEvent);
        };
        _skInkCanvas = skInkCanvas;
    }

    private X11PlatformThreading X11PlatformThreading { get; }
    
    private readonly X11Info _x11Info;
    private readonly IntPtr _mainWindowHandle;
    private readonly SKBitmap _skBitmap;
    private readonly SKCanvas _skCanvas;
    private XImage _image;
    private SkInkCanvas _skInkCanvas;
    private IntPtr GC { get; }
    
    private unsafe XImage CreateImage()
    {
        const int bytePerPixelCount = 4; // RGBA 一共4个 byte 长度
        var bitPerByte = 8;
        
        var bitmapWidth = _skBitmap.Width;
        var bitmapHeight = _skBitmap.Height;
        
        var img = new XImage();
        int bitsPerPixel = bytePerPixelCount * bitPerByte;
        img.width = bitmapWidth;
        img.height = bitmapHeight;
        img.format = 2; //ZPixmap;
        img.data = _skBitmap.GetPixels();
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

    public IntPtr X11InkWindowIntPtr { get; }
    
    public Task InvokeAsync(Action<SkInkCanvas> action)
    {
       return X11PlatformThreading.InvokeAsync(() =>
       {
           action(_skInkCanvas);
       }, X11InkWindowIntPtr);
    }
    
    public void Expose(XExposeEvent exposeEvent)
    {
        XPutImage(_x11Info.Display, X11InkWindowIntPtr, GC, ref _image, exposeEvent.x, exposeEvent.y, exposeEvent.x, exposeEvent.y, (uint) exposeEvent.width,
            (uint) exposeEvent.height);
    }
}

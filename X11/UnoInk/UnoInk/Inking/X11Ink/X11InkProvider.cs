using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;
using Windows.Foundation;
using CPF.Linux;
using ReewheaberekaiNayweelehe;
using SkiaSharp;
using Uno.UI.Xaml;
using static CPF.Linux.XFixes;
using static CPF.Linux.XLib;
using static CPF.Linux.ShapeConst;
using Uno.Extensions;
using UnoInk.X11Platforms;
using UnoInk.X11Platforms.Threading;
using X11Info = UnoInk.X11Platforms.X11Info;

namespace UnoInk.X11Ink;

//interface IX11WindowManager

[SupportedOSPlatform("Linux")]
internal class X11InkProvider : X11Application
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
    }

    [MemberNotNull(nameof(_x11InkWindow))]
    public void Start(Window unoWindow)
    {
        base.Start();

#if HAS_UNO
        var x11Window = unoWindow.GetNativeWindow()!;
#else
        var type = unoWindow.GetType();
        var nativeWindowPropertyInfo = type.GetProperty("NativeWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        var x11Window = nativeWindowPropertyInfo!.GetMethod!.Invoke(unoWindow, null)!;
#endif
        // Uno.WinUI.Runtime.Skia.X11.X11Window
        var x11WindowType = x11Window.GetType();

        var x11WindowIntPtr =
            (IntPtr) x11WindowType.GetProperty("Window", BindingFlags.Instance | BindingFlags.Public)!.GetMethod!.Invoke(
                x11Window, null)!;

        //Console.WriteLine($"Uno 窗口句柄 {x11WindowIntPtr}");

        var x11InkWindow = new X11InkWindow(this, x11WindowIntPtr);
        _x11InkWindow = x11InkWindow;
    }

    public X11InkWindow InkWindow
    {
        get
        {
            EnsureX11Start();
            return _x11InkWindow;
        }
    }

    private X11InkWindow? _x11InkWindow;

    [MemberNotNull(nameof(_x11InkWindow))]
    private void EnsureX11Start()
    {
        if (_x11InkWindow is null)
        {
            throw new InvalidOperationException();
        }
    }

    internal override void DispatchEvent(XEvent @event)
    {
        if (_x11InkWindow is null)
        {
            // 这里可能是创建窗口内部进来的，比如全屏
            return;
        }

        if (@event.type == XEventName.Expose)
        {
            if (@event.ExposeEvent.window == InkWindow.X11InkWindowIntPtr)
            {
                InkWindow.Expose(@event.ExposeEvent);
                return;
            }
        }

        base.DispatchEvent(@event);
    }
}

[SupportedOSPlatform("Linux")]
class X11InkWindow : X11Window
{
    public X11InkWindow(X11Application application, IntPtr mainWindowHandle) : base(application,
        new X11WindowCreateInfo()
        {
            IsFullScreen = true
        })
    {
        application.EnsureStart();
        X11PlatformThreading = application.X11PlatformThreading;
        var x11Info = application.X11Info;
        _x11Info = x11Info;
        _mainWindowHandle = mainWindowHandle;

        var xDisplayWidth = x11Info.XDisplayWidth;
        var xDisplayHeight = x11Info.XDisplayHeight;

        // 设置不接受输入
        // 这样输入穿透到后面一层里，由后面一层将内容上报上来
        SetClickThrough();

        // 设置一定放在输入的窗口上方
        SetOwner(mainWindowHandle);

        ShowActive();

        // 进入全屏
        EnterFullScreen(topmost: false/*这里必须设置为false否则UNO窗口将不会渲染*/);

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
            XSendEvent(x11Info.Display, X11InkWindowIntPtr, propagate: false, new IntPtr((int) (EventMask.ExposureMask)),
                ref xEvent);
        };
        SkInkCanvas = skInkCanvas;
    }

    public X11PlatformThreading X11PlatformThreading { get; }

    private readonly X11Info _x11Info;
    private readonly IntPtr _mainWindowHandle;
    private readonly SKBitmap _skBitmap;
    private readonly SKCanvas _skCanvas;
    private XImage _image;
    public SkInkCanvas SkInkCanvas { get; }

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

    public IntPtr X11InkWindowIntPtr => X11WindowIntPtr;

    public Task InvokeAsync(Action<SkInkCanvas> action)
    {
        return X11PlatformThreading.InvokeAsync(() => { action(SkInkCanvas); }, X11InkWindowIntPtr);
    }

    public void Expose(XExposeEvent exposeEvent)
    {
        XPutImage(_x11Info.Display, X11InkWindowIntPtr, GC, ref _image, exposeEvent.x, exposeEvent.y, exposeEvent.x,
            exposeEvent.y, (uint) exposeEvent.width,
            (uint) exposeEvent.height);
    }
}

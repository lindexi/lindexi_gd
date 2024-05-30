using System.Runtime.Versioning;
using CPF.Linux;
using SkiaSharp;
using UnoInk.Inking.InkCore;
using UnoInk.Inking.InkCore.Interactives;
using UnoInk.Inking.X11Platforms;
using UnoInk.Inking.X11Platforms.Input;
using UnoInk.Inking.X11Platforms.Threading;

namespace UnoInk.Inking.X11Ink;

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
            new SkInkCanvas(_skCanvas, _skBitmap);
        //skInkCanvas.ApplicationDrawingSkBitmap = _skBitmap;
        //skInkCanvas.SetCanvas(_skCanvas);
        
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
            XLib.XSendEvent(x11Info.Display, X11InkWindowIntPtr, propagate: false, new IntPtr((int) (EventMask.ExposureMask)),
                ref xEvent);
        };
        SkInkCanvas = skInkCanvas;
        
        var modeInputDispatcher = new ModeInputDispatcher();
        modeInputDispatcher.AddInputProcessor(skInkCanvas);
        ModeInputDispatcher = modeInputDispatcher;
        X11DeviceInputManager = new X11DeviceInputManager(_x11Info);
    }
    
    public X11DeviceInputManager X11DeviceInputManager { get; }
    
    public X11PlatformThreading X11PlatformThreading { get; }
    
    private readonly X11InfoManager _x11Info;
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
        XLib.XInitImage(ref img);
        
        return img;
    }
    
    public IntPtr X11InkWindowIntPtr => X11WindowIntPtr;
    public ModeInputDispatcher ModeInputDispatcher { get; }
    
    public Task InvokeAsync(Action<SkInkCanvas> action)
    {
        return X11PlatformThreading.InvokeAsync(() => { action(SkInkCanvas); }, X11InkWindowIntPtr);
    }
    
    public void Expose(XExposeEvent exposeEvent)
    {
        XLib.XPutImage(_x11Info.Display, X11InkWindowIntPtr, GC, ref _image, exposeEvent.x, exposeEvent.y, exposeEvent.x,
            exposeEvent.y, (uint) exposeEvent.width,
            (uint) exposeEvent.height);
    }
}

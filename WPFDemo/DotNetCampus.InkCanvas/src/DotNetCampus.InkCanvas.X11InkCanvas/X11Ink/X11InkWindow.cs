using System.Runtime.Versioning;
using DotNetCampus.InkCanvas.X11InkCanvas.Input;
using DotNetCampus.InkCanvas.X11InkCanvas.Renders;
using DotNetCampus.Inking.Settings;
using SkiaSharp;
using UnoInk.Inking.InkCore;
using UnoInk.Inking.InkCore.Interactives;
using UnoInk.Inking.InkCore.Settings;
using X11ApplicationFramework.Apps;
using X11ApplicationFramework.Apps.Threading;
using X11ApplicationFramework.Natives;

namespace DotNetCampus.InkCanvas.X11InkCanvas.X11Ink;

[SupportedOSPlatform("Linux")]
class X11InkWindow : X11Window
{
    /// <summary>
    /// 创建一个 X11 笔迹窗口
    /// </summary>
    /// <param name="application"></param>
    /// <param name="mainWindowHandle"></param>
    /// <param name="enableInput">是否允许从 X11 获取输入</param>
    public X11InkWindow(X11Application application, IntPtr mainWindowHandle, bool enableInput = false) : this
    (
        application,
        mainWindowHandle,
        enableInput,
        new X11WindowCreateInfo()
        {
            IsFullScreen = true,

            Width = application.X11Info.XDisplayWidth,
            Height = application.X11Info.XDisplayHeight
        }
    )
    {
    }

    /// <summary>
    /// 创建一个 X11 笔迹窗口
    /// </summary>
    /// <param name="application"></param>
    /// <param name="mainWindowHandle"></param>
    /// <param name="enableInput">是否允许从 X11 获取输入</param>
    /// <param name="info"></param>
    public X11InkWindow(X11Application application, IntPtr mainWindowHandle, bool enableInput,
        X11WindowCreateInfo info)
        : base(application, info)
    {
        X11PlatformThreading = application.X11PlatformThreading;
        var x11Info = application.X11Info;
        _x11Info = x11Info;
        _mainWindowHandle = mainWindowHandle;
        _enableInput = enableInput;

        var windowWidth = info.Width;
        var windowHeight = info.Height;

        // 放在显示窗口之前进行 XIQueryDevice 不会让窗口停止渲染，否则将会在 XIQueryDevice 方法卡住
        X11DeviceInputManager = new X11DeviceInputManager(_x11Info, X11WindowIntPtr);

        // 在 SetClickThrough 之前注册触摸，这样也不会让下层窗口收不到触摸
        var pointerDevice = X11DeviceInputManager.PointerDevice;
        if (pointerDevice != null)
        {
            // 如果在 SetClickThrough 之后注册触摸，将会让当前窗口收不到触摸，且下层窗口也收不到触摸，估计是 X11 的坑
            RegisterMultiTouch(pointerDevice);
        }

        BusinessShow();

        var skBitmap = new SKBitmap(windowWidth, windowHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

        _skBitmap = skBitmap;
        var skCanvas = new SKCanvas(skBitmap);
        _skCanvas = skCanvas;

        if (ShouldDrawDebugImage)
        {
            skCanvas.Clear(SKColors.Transparent);
            using var skPaint = new SKPaint();
            skPaint.IsAntialias = true;
            skPaint.Color = SKColors.Black;
            skPaint.StrokeWidth = 10;
            skPaint.Style = SKPaintStyle.Stroke;
            skCanvas.DrawLine(0, 0, windowWidth, windowHeight, skPaint);
            skCanvas.DrawLine(0, windowHeight, windowWidth, 0, skPaint);
            skCanvas.Flush();
        }

        var xImageProxy = new XImageProxy(_skBitmap);
        _xImageProxy = xImageProxy;
        //_image = xImageProxy.Image;

        _x11RenderManager = new X11RenderManager(application, X11InkWindowIntPtr,
            new SKBitmapBackendRenderContext(_skBitmap));

        var skInkCanvas = // new SkInkCanvas(_skCanvas, _skBitmap);
            new SkInkCanvas(_skCanvas, _skBitmap);
        //skInkCanvas.ApplicationDrawingSkBitmap = _skBitmap;
        //skInkCanvas.SetCanvas(_skCanvas);

        // 以下配置会被 BoardInkLayer 覆盖，但这里依然保持，是为了独立业务
        //skInkCanvas.Settings = skInkCanvas.Settings with
        //{
        //    AutoSoftPen = false,
        //    //EnableEraserGesture = false,
        //    DynamicRenderType = InkCanvasDynamicRenderTipStrokeType.RenderAllTouchingStrokeWithClip,

        //    // 尝试修复丢失按下的点
        //    ShouldDrawStrokeOnDown = true,

        //    CleanStrokeSettings = new CleanStrokeSettings()
        //    {
        //        ShouldDrawBackground = false,
        //        ShouldUpdateBackground = true,
        //    },

        //    // 丢点策略
        //    DropPointSettings = skInkCanvas.Settings.DropPointSettings with
        //    {
        //        DropPointStrategy = DropPointStrategy.Aggressive
        //    }
        //};

        // 统一为调试模式
        skInkCanvas.Settings = SkInkCanvasSettings.DebugSettings(skInkCanvas.Settings);

        skInkCanvas.RenderBoundsChanged += (sender, rect) =>
        {
            var x = (int) rect.X;
            var y = (int) rect.Y;
            var width = (int) rect.Width;
            var height = (int) rect.Height;

            _x11RenderManager.RequestRender(SKRectI.Create(x, y, width, height));
        };

        skInkCanvas.RequestDispatcher += (sender, action) =>
        {
            X11PlatformThreading.TryEnqueue(action, X11WindowIntPtr);
        };

        SkInkCanvas = skInkCanvas;

        // 输入调度器，既可以禁用 X11 窗口本身的输入接收，完全靠其他 UI 框架调度输入进来，也可以就在 X11 窗口处理所有的输入
        var modeInputDispatcher = new InkingModeInputDispatcher();
        modeInputDispatcher.AddInputProcessor(skInkCanvas);
        modeInputDispatcher.AddInputProcessor(skInkCanvas.SkInkCanvasManipulationManager);

        ModeInputDispatcher = modeInputDispatcher;

        // 这里为 X11 窗口自己接收输入时的输入调度
        HandleInput(X11DeviceInputManager);
    }

    private SkInkCanvasSettings Settings => SkInkCanvas.Settings;

    private void HandleInput(X11DeviceInputManager touchInputManager)
    {
        var modeInputDispatcher = ModeInputDispatcher;
        touchInputManager.DevicePressed += (sender, args) =>
        {
            //if (IsDrawLineMode)
            //{
            //    _lastPoint = ((int) args.Position.X, (int) args.Position.Y);
            //}
            //else
            {
                TryEnterEraserGestureMode(in args, isDown: true);
                modeInputDispatcher.Down(args.ToModeInputArgs(Settings.IgnorePressure));
            }
        };
        
        touchInputManager.DeviceMoved += (sender, args) =>
        {
            //StaticDebugLogger.WriteLine($"DeviceMoved Id={args.Id} {args.Position.X:0.00},{args.Position.Y:0.00} WH:{args.Point.PixelWidth:0.00},{args.Point.PixelHeight:0.00}");
            //if (IsDrawLineMode)
            //{
            //    var (x, y) = ((int) args.Position.X, (int) args.Position.Y);
            //    XDrawLine(Display, Window, GC, _lastPoint.X, _lastPoint.Y, x, y);
            //    _lastPoint = (x, y);
            //}
            //else
            {
                TryEnterEraserGestureMode(in args, isDown: false);
                modeInputDispatcher.Move(args.ToModeInputArgs(Settings.IgnorePressure));
            }
        };
        
        touchInputManager.DeviceReleased += (sender, args) =>
        {
            //if (IsDrawLineMode)
            //{
                
            //}
            //else
            {
                modeInputDispatcher.Up(args.ToModeInputArgs(Settings.IgnorePressure));
            }
        };
        
        void TryEnterEraserGestureMode(in DeviceInputArgs args, bool isDown)
        {

        }
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
    public InkingModeInputDispatcher ModeInputDispatcher { get; }
    
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

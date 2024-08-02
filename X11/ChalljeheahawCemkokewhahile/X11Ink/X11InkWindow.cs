using CPF.Linux;
using ReewheaberekaiNayweelehe;
using SkiaInkCore.Settings;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using SkiaInkCore.Interactives;
using UnoInk.Inking.X11Platforms;
using UnoInk.Inking.X11Platforms.Input;
using UnoInk.Inking.X11Platforms.Threading;

namespace ChalljeheahawCemkokewhahile.X11Ink;

[SupportedOSPlatform("Linux")]
class X11InkWindow : X11Window
{
    /// <summary>
    /// 创建一个 X11 笔迹窗口
    /// </summary>
    /// <param name="application"></param>
    /// <param name="mainWindowHandle"></param>
    /// <param name="enableInput">是否允许从 X11 获取输入</param>
    public X11InkWindow(X11Application application, IntPtr mainWindowHandle, bool enableInput = false) : base(application,
        new X11WindowCreateInfo()
        {
            IsFullScreen = true
        })
    {
        X11PlatformThreading = application.X11PlatformThreading;
        var x11Info = application.X11Info;
        _x11Info = x11Info;
        _mainWindowHandle = mainWindowHandle;
        _enableInput = enableInput;

        var xDisplayWidth = x11Info.XDisplayWidth;
        var xDisplayHeight = x11Info.XDisplayHeight;

        // 放在显示窗口之前进行 XIQueryDevice 不会让窗口停止渲染，否则将会在 XIQueryDevice 方法卡住
        X11DeviceInputManager = new X11DeviceInputManager(_x11Info);

        // 在 SetClickThrough 之前注册触摸，这样也不会让下层窗口收不到触摸
        var pointerDevice = X11DeviceInputManager.PointerDevice;
        if (pointerDevice != null)
        {
            // 如果在 SetClickThrough 之后注册触摸，将会让当前窗口收不到触摸，且下层窗口也收不到触摸，估计是 X11 的坑
            RegisterMultiTouch(pointerDevice);
        }

        BusinessShow();

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
        skInkCanvas.Settings = SkInkCanvasSettings.DebugSettings(skInkCanvas.Settings);

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

            var currentRect = SKRectI.Create((int) rect.X, (int) rect.Y, (int) rect.Width, (int) rect.Height);
            if (_renderRect is null)
            {
                _renderRect = currentRect;
            }
            else
            {
                var t = _renderRect.Value;
                t.Union(currentRect);
                _renderRect = t;
            }

            if (_isPushExpose)
            {
                return;
            }

            _isPushExpose = true;

            var xEvent = new XEvent
            {
                ExposeEvent =
                {
                    type = XEventName.Expose,
                    send_event = true,
                    window = X11InkWindowIntPtr,
                    count = 1,
                    display = x11Info.Display,
                    height = currentRect.Height,
                    width = currentRect.Width,
                    x = currentRect.Left,
                    y = currentRect.Top
                }
            };
            // [Xlib Programming Manual: Expose Events](https://tronche.com/gui/x/xlib/events/exposure/expose.html )
            XLib.XSendEvent(x11Info.Display, X11InkWindowIntPtr, propagate: false, new IntPtr((int) (EventMask.ExposureMask)),
                ref xEvent);
        };
        SkInkCanvas = skInkCanvas;

        var modeInputDispatcher = new InkingModeInputDispatcher();
        modeInputDispatcher.AddInputProcessor(skInkCanvas);
        modeInputDispatcher.AddInputProcessor(skInkCanvas.SkInkCanvasManipulationManager);
        ModeInputDispatcher = modeInputDispatcher;
        HandleInput(X11DeviceInputManager);
    }

    /// <summary>
    /// 业务上的显示窗口，包括设置窗口穿透和设置全屏
    /// </summary>
    private void BusinessShow()
    {
        if(!_enableInput)
        {
            // 设置不接受输入
            // 这样输入穿透到后面一层里，由后面一层将内容上报上来
            SetClickThrough();
        }

        if (_mainWindowHandle != IntPtr.Zero)
        // 设置一定放在输入的窗口上方
        {
            SetOwner(_mainWindowHandle);
        }

        ShowActive();

        // 进入全屏
        EnterFullScreen(topmost: false/*这里必须设置为false否则UNO窗口将不会渲染*/);
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
            if (modeInputDispatcher.IsInputStart && modeInputDispatcher.InputDuring > Settings.DisableEnterEraserGestureAfterInputDuring)
            {
                // 如果写的时间太久了，那就不能进入手势橡皮擦模式了
                return;
            }

            if (!isDown && !modeInputDispatcher.IsInputStart)
            {
                // 非按下的情况下，触摸还没开始，那就是证明被 Leave 了，不能启动橡皮擦
                // 修复手势橡皮擦进入工具条时，先触发 Leave 里面，符合预期的进行结束手势橡皮擦。然而后续居然又继续收到 Move 事件，导致判断橡皮擦逻辑工作，再次错误进入了手势橡皮擦模式
                return;
            }

            // 经过之前的埋点分析，采样一些老师，橡皮擦面积都是 6-7 厘米，于是这里就取最小的 6 厘米，充分具备灵敏度，可以方便进行擦掉
            if (args.PhysicalWidth > Settings.MinEraserGesturePhysicalSizeCm)
            {
                // 大于 给定 厘米视为手势擦
                if (!SkInkCanvas.IsInEraserGestureMode)
                {
                    // 如果没有进入橡皮擦模式则进入
                    SkInkCanvas.EnterEraserGestureMode(args.ToModeInputArgs(Settings.IgnorePressure));
                }
            }
        }
    }

    public X11DeviceInputManager X11DeviceInputManager { get; }

    public X11PlatformThreading X11PlatformThreading { get; }

    private readonly X11InfoManager _x11Info;
    private readonly IntPtr _mainWindowHandle;
    private readonly bool _enableInput;
    private readonly SKBitmap _skBitmap;
    private readonly SKCanvas _skCanvas;
    private XImage _image;
    public SkInkCanvas SkInkCanvas { get; }

    public void ReShow()
    {
        //StaticDebugLogger.WriteLine("X11InkWindow.ReShow");
        BusinessShow();
    }

    private XImage CreateImage()
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

    private bool _isPushExpose = false;
    private SKRectI? _renderRect = null;

    public void Expose(XExposeEvent exposeEvent)
    {
        _isPushExpose = false;
        var exposeRect = SKRectI.Create(exposeEvent.x, exposeEvent.y, exposeEvent.width, exposeEvent.height);
        if (_renderRect != null)
        {
            exposeRect.Union(_renderRect.Value);
        }

        var x = exposeRect.Left;
        var y = exposeRect.Top;

        //XLib.XPutImage(_x11Info.Display, X11InkWindowIntPtr, GC, ref _image, exposeEvent.x, exposeEvent.y, exposeEvent.x,
        //    exposeEvent.y, (uint) exposeEvent.width,
        //    (uint) exposeEvent.height);
        XLib.XPutImage(_x11Info.Display, X11InkWindowIntPtr, GC, ref _image,
            x, y, 
            x, y, 
            (uint) exposeRect.Width,
            (uint) exposeRect.Height);
    }
}

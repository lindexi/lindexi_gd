using DotNetCampus.InkCanvas.X11InkCanvas.Input;
using DotNetCampus.InkCanvas.X11InkCanvas.Renders;
using DotNetCampus.Inking.Settings;

using SkiaSharp;

using System.Diagnostics;
using System.Runtime.Versioning;

using UnoInk.Inking.InkCore;
using UnoInk.Inking.InkCore.Interactives;
using UnoInk.Inking.InkCore.Settings;

using X11ApplicationFramework.Apps;
using X11ApplicationFramework.Apps.Threading;
using X11ApplicationFramework.Natives;

namespace DotNetCampus.InkCanvas.X11InkCanvas.X11Ink;

[SupportedOSPlatform("Linux")]
class X11InkWindow : X11Window, IDisposable
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

    ~X11InkWindow()
    {
        Dispose();
    }

    /// <summary>
    /// 关联的屏幕的物理宽度
    /// </summary>
    public double? AssociatedMonitorPhysicalWidthCentimetre
    {
        get => X11DeviceInputManager.AssociatedMonitorPhysicalWidthCentimetre;
        set => X11DeviceInputManager.AssociatedMonitorPhysicalWidthCentimetre = value;
    }

    /// <summary>
    /// 关联的屏幕的物理高度
    /// </summary>
    public double? AssociatedMonitorPhysicalHeightCentimetre
    {
        get => X11DeviceInputManager.AssociatedMonitorPhysicalHeightCentimetre;
        set => X11DeviceInputManager.AssociatedMonitorPhysicalHeightCentimetre = value;
    }

    /// <summary>
    /// 关联的屏幕的像素宽度
    /// </summary>
    public double? AssociatedMonitorPixelWidth
    {
        get => X11DeviceInputManager.AssociatedMonitorPixelWidth;
        set => X11DeviceInputManager.AssociatedMonitorPixelWidth = value;
    }

    /// <summary>
    /// 关联的屏幕的像素高度
    /// </summary>
    public double? AssociatedMonitorPixelHeight
    {
        get => X11DeviceInputManager.AssociatedMonitorPixelHeight;
        set => X11DeviceInputManager.AssociatedMonitorPixelHeight = value;
    }

#if DEBUG
    /// <summary>
    /// 在 RenderBoundsChanged 事件中，是否在曝光之前推送图片
    /// </summary>
    /// 调试代码
    public bool PutImageBeforeExposeOnRenderBoundsChanged
    {
        get => _x11RenderManager.PutImageBeforeExposeOnRequestRender;
        set => _x11RenderManager.PutImageBeforeExposeOnRequestRender = value;
    }
#endif

    public bool ShouldDrawDebugImage { get; set; }

#if DEBUG
        = true;
#endif

    internal X11RenderManager X11RenderManager => _x11RenderManager;

    private readonly X11RenderManager _x11RenderManager;

    /// <summary>
    /// 业务上的显示窗口，包括设置窗口穿透和设置全屏
    /// </summary>
    private void BusinessShow()
    {
        if (!_enableInput)
        {
            // 设置不接受输入
            // 这样输入穿透到后面一层里，由后面一层将内容上报上来
            SetClickThrough();
        }

        // 默认都不要在任务栏显示
        ShowTaskbarIcon(false);

        bool shouldResetOwnerByUnmapAndMap = _mainWindowHandle != IntPtr.Zero && IsShowing;

        if (shouldResetOwnerByUnmapAndMap)
        {
            Debug.Assert(_mainWindowHandle != IntPtr.Zero);

            ResetOwnerByUnmapAndMap(_mainWindowHandle);

            EnterFullScreen(topmost: false /*这里必须设置为false否则UNO窗口将不会渲染*/);
        }
        else
        {
            // 在麒麟系统上，需要在 XMapWindow 之前设置全屏。否则将只应用全屏的窗口样式，但窗口实际尺寸没有全屏。在 1920x1080 的屏幕上，窗口尺寸是 1920x1040 的大小，刚好就是任务栏的高度
            // 进入全屏
            EnterFullScreen(topmost: false /*这里必须设置为false否则UNO窗口将不会渲染*/);

            if (_mainWindowHandle != IntPtr.Zero)
            {
                SetOwner(_mainWindowHandle);
            }

            ShowActive();
        }
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
            if (modeInputDispatcher.IsInputStart &&
                modeInputDispatcher.InputDuring > Settings.DisableEnterEraserGestureAfterInputDuring)
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

            // 经过之前的埋点分析，采样400万老师，橡皮擦面积都是 6-7 厘米，于是这里就取最小的 6 厘米，充分具备灵敏度，可以方便进行擦掉
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
    //private XImage _image;
    private XImageProxy _xImageProxy;
    public SkInkCanvas SkInkCanvas { get; }

    //public void EnsureSetOwner()
    //{
    //    _ = ResetOwnerAsync(_mainWindowHandle);
    //}

    public void ReShow()
    {
        StaticDebugLogger.WriteLine("X11InkWindow.ReShow");
        BusinessShow();
    }

    public IntPtr X11InkWindowIntPtr => X11WindowIntPtr;
    public InkingModeInputDispatcher ModeInputDispatcher { get; }

    public Task InvokeAsync(Action<SkInkCanvas> action)
    {
        return X11PlatformThreading.InvokeAsync(() => { action(SkInkCanvas); }, X11InkWindowIntPtr);
    }

    public void Expose(XExposeEvent exposeEvent)
    {
        //XLib.XPutImage(_x11Info.Display, X11InkWindowIntPtr, GC, ref _image, exposeEvent.x, exposeEvent.y,
        //    exposeEvent.x,
        //    exposeEvent.y, (uint)exposeEvent.width,
        //    (uint)exposeEvent.height);

        //PutImage(exposeEvent.x, exposeEvent.y, exposeEvent.width, exposeEvent.height);
        //#if DEBUG
        //        Stopwatch stopwatch = Stopwatch.StartNew();
        //#endif
        _x11RenderManager.OnExpose(exposeEvent);
        //#if DEBUG
        //        stopwatch.Stop();
        //        StaticDebugLogger.WriteLine($"曝光处理 {stopwatch.ElapsedMilliseconds}");
        //#endif
    }

    //private void PutImage(int x, int y, int width, int height)
    //{
    //    //foreach (ISpriteAdorner spriteAdorner in SpriteAdorners)
    //    //{
    //    //    // 先绘制再推送，防止推送的时候绘制耗时
    //    //    spriteAdorner.Draw(_skBitmap);
    //    //}

    //    XLib.XPutImage(_x11Info.Display, X11InkWindowIntPtr, GC, ref _image, x, y, x, y, (uint)width, (uint)height);

    //    // 使用以下方式进行推送，会导致闪烁，会先显示前面 XPutImage 输出的内容，再显示后面 XPutImage 输出的内容。从而导致存在的闪烁
    //    //// 先推送应用的图片，再推送所有的所有的附加精灵层，可以使用精灵方式实现镂空
    //    //foreach (ISpriteAdorner spriteAdorner in SpriteAdorners)
    //    //{
    //    //   using var xImageProxy = new XImageProxy(spriteAdorner.OutputBitmap);
    //    //   XImage image = xImageProxy.Image;
    //    //   SKRectI bounds = spriteAdorner.Bounds;

    //    //   if (bounds.Width > spriteAdorner.OutputBitmap.Width ||
    //    //       bounds.Height > spriteAdorner.OutputBitmap.Height)
    //    //   {
    //    //       throw new ArgumentException();
    //    //   }

    //    //   XLib.XPutImage(_x11Info.Display, X11InkWindowIntPtr, GC, ref image, 0, 0, bounds.Left, bounds.Top, (uint) bounds.Width, (uint) bounds.Height);
    //    //}
    //}

    public void ResetSpriteAdornerList(IList<ISpriteAdorner> list)
    {
        _x11RenderManager.SpriteAdorners.Clear();

        for (int index = 0; index < list.Count; index++)
        {
            ISpriteAdorner spriteAdorner = list[index];
            SKRectI bounds = spriteAdorner.Bounds;
            IApplicationBackendRenderContext context = _x11RenderManager.Context;

            if (bounds.Right > context.PixelWidth || bounds.Bottom > context.PixelHeight)
            {
#if DEBUG
                throw new ArgumentException($"Bounds Out of range. SpriteAdorner Index={index};bounds.Right > context.PixelWidth || bounds.Bottom > context.PixelHeight ； bounds.Right={bounds.Right};context.PixelWidth={context.PixelWidth};bounds.Bottom={bounds.Bottom};context.PixelHeight={context.PixelHeight}; 尺寸超过范围");
#else
                continue;
#endif
            }
            _x11RenderManager.SpriteAdorners.Add(spriteAdorner);
        }
    }

    public List<ISpriteAdorner> SpriteAdorners => _x11RenderManager.SpriteAdorners;

    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _skBitmap.Dispose();
        _skCanvas.Dispose();
        _xImageProxy.Dispose();

        global::System.GC.SuppressFinalize(this);
    }

    protected override void OnClosed()
    {
        Dispose();
    }
}

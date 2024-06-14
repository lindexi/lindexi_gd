using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Input;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using UnoInk.Inking.InkCore;
using UnoInk.Inking.InkCore.Interactives;
using UnoInk.Inking.X11Ink;
using UnoInk.Inking.X11Platforms;
using UnoInk.Inking.X11Platforms.Threading;
//using Uno.Skia;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UnoInk.UnoInkCore;

public sealed partial class UnoInkCanvasUserControl : UserControl
{
    public UnoInkCanvasUserControl(Window currentWindow) : this()
    {
        _currentWindow = currentWindow;
    }
    
    public UnoInkCanvasUserControl()
    {
        this.InitializeComponent();

        Loaded += MainPage_Loaded;

        
        // 由于 SkiaVisual 没有明确的优势，不能解决同步渲染闪烁问题
        // 也会导致每次都渲染所有静态笔迹，笔迹数量多了会卡
        // 因此这里不使用 SkiaVisual 绘制，后续可以考虑笔迹元素再这么使用
        //#if HAS_UNO
        //        var skiaVisual = SkiaVisual.CreateAndInsertTo(this);
        //        skiaVisual.OnDraw += SkiaVisual_OnDraw;
        //#endif
    }
    
    
    private readonly Window? _currentWindow;

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsLinux())
        {
            if (_currentWindow is null)
            {
                throw new InvalidOperationException($"只有传入窗口时，才能使用覆盖窗口的 X11 动态笔迹方式");
            }

            if (_x11InkProvider == null)
            {
                // 尝试修复 UNO 停止渲染，但也可能是 UNO 自己的坑，就是界面不显示
                // 原先在 Avalonia 也有这样的问题
                await Task.Delay(TimeSpan.FromSeconds(1));
                _x11InkProvider = new X11InkProvider();

                _x11InkProvider.Start(_currentWindow);

                _dispatcherRequiring =
                    new DispatcherRequiring(InvokeInk, _x11InkProvider.InkWindow.GetDispatcher());

                var skInkCanvas = _x11InkProvider.InkWindow.SkInkCanvas;
                skInkCanvas.StrokesCollected += SkInkCanvas_StrokesCollected;

                Console.WriteLine($"完成初始化");
                InitTextBlock.Text = "完成初始化";
            }
        }
    }

    private void SkInkCanvas_StrokesCollected(object? sender, StrokeCollectionInfo e)
    {
        StaticDebugLogger.WriteLine($"SkInkCanvas_StrokesCollected InkId={e.InkId.Value}");

        // 这是 X11 线程进入的
        lock (_locker)
        {
            StrokeInfoList.Add(e);
        }

        //InvalidateRedraw();
    }

    private List<StrokeCollectionInfo> StrokeInfoList { get; } = new List<StrokeCollectionInfo>();
    private readonly object _locker = new object();


    private DispatcherRequiring? _dispatcherRequiring;

    private bool _isDown = false;

    private void InkCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        //var pointerPoint = e.GetCurrentPoint(InkCanvas);
        //Point position = pointerPoint.Position;

        //var inkInfo = new InkInfo();
        //_inkInfoCache[e.Pointer.PointerId] = inkInfo;
        //inkInfo.PointList.Add(position);
        //DrawStroke(inkInfo);
        //DrawInNative(position);
        _isDown = true;
        //_firstMove = true;
        //LogTextBlock.Text += $"按下： {e.Pointer.PointerId}\r\n";
        //LogTextBlock.Text += $"当前按下点数： {_inkInfoCache.Count} [{string.Join(',', _inkInfoCache.Keys)}]";
        //Console.WriteLine($"按下： {e.Pointer.PointerId}");
        var inputInfo = ToModeInputArgs(e);
        // 输入时不再立刻记录，防止记录到不正确的值
        //_lastInkingInputInfo = inputInfo;
        //_dispatcherRequiring?.Require();
        InvokeAsync(canvas =>
        {
            //StaticDebugLogger.WriteLine($"执行按下 {inputInfo.Position}");
            canvas.ModeInputDispatcher.Down(inputInfo);
        });
    }

    private ModeInputArgs ToModeInputArgs(PointerRoutedEventArgs args)
    {
        var currentPoint = args.GetCurrentPoint(this);
        var stylusPoint = ToStylusPoint(currentPoint);
        var modeInputArgs = new ModeInputArgs((int) args.Pointer.PointerId, stylusPoint, currentPoint.Timestamp);
        return modeInputArgs;
    }

    private static StylusPoint ToStylusPoint(PointerPoint currentPoint)
    {
        var stylusPoint = new StylusPoint(currentPoint.Position.X, currentPoint.Position.Y, currentPoint.Properties.Pressure);
        return stylusPoint;
    }


    //private InkingInputInfo ToInkingInputInfo(PointerRoutedEventArgs args)
    //{
    //    var currentPoint = args.GetCurrentPoint(this);

    //    var stylusPoint = new StylusPoint(currentPoint.Position.X, currentPoint.Position.Y, currentPoint.Properties.Pressure);
    //    return new InkingInputInfo((int) args.Pointer.PointerId, stylusPoint, currentPoint.Timestamp);
    //}

    //private Point _lastPoint;

    //private bool _firstMove = true;

    class MoveInputInfo
    {
        public ModeInputArgs InputArgs { set; get; }
        public List<StylusPoint>? StylusPointList { get; set; }
    }
    private MoveInputInfo? _inputInfo;

    private void InkCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDown)
        {
            StaticDebugLogger.WriteLine($"没有按下就移动！！！InkCanvas_OnPointerMoved");
            return;
        }

        //if (_firstMove)
        //{
        //    StaticDebugLogger.WriteLine($"InkCanvas_OnPointerMoved");
        //}

        //_firstMove = false;

        //var currentPoint = e.GetCurrentPoint(this);
        //Point position = currentPoint.Position;

        //var length = Math.Pow(position.X - _lastPoint.X, 2) + Math.Pow(position.Y - _lastPoint.Y, 2);
        //if (length < 10)
        //{
        //    // 简单的丢点
        //    return;
        //}

        //_lastPoint = position;
        //if (_inkInfoCache.TryGetValue(e.Pointer.PointerId, out var inkInfo))
        //{
        //    var pointerPoint = e.GetCurrentPoint(InkCanvas);
        //    Point position = pointerPoint.Position;

        //    inkInfo.PointList.Add(position);
        //    //DrawStroke(inkInfo);
        //    DrawInNative(position);
        //}

        lock (_locker)
        {
            if (_inputInfo is null)
            {
                var modeInputArgs = ToModeInputArgs(e);
                _inputInfo = new MoveInputInfo
                {
                    InputArgs = modeInputArgs
                };
            }
            else
            {
                if (_inputInfo.StylusPointList is null)
                {
                    _inputInfo.StylusPointList = new List<StylusPoint>(2)
                    {
                        _inputInfo.InputArgs.StylusPoint
                    };
                    _inputInfo.InputArgs = _inputInfo.InputArgs with
                    {
                        StylusPointList = _inputInfo.StylusPointList
                    };
                }

                var currentPoint = e.GetCurrentPoint(this);
                var stylusPoint = ToStylusPoint(currentPoint);
                _inputInfo.StylusPointList.Add(stylusPoint);
            }
        }

        _dispatcherRequiring?.Require();
        //InvokeAsync(canvas => canvas.Move(ToInkingInputInfo(e)));
    }

    private void InvokeInk()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        //if (_lastInkingInputInfo is null)
        //{
        //    return;
        //}

        if (_x11InkProvider is null)
        {
            throw new InvalidOperationException();
        }

        MoveInputInfo inputInfo;

        lock (_locker)
        {
            if (_inputInfo is null)
            {
                // 可能连续进入两次
                //Debug.Fail("进入这里时，一定存在当前的输入");
                return;
            }

            inputInfo = _inputInfo;
            _inputInfo = null;
        }

        //StaticDebugLogger.WriteLine($"执行移动 {inputInfo.InputArgs.Position} Count={inputInfo.StylusPointList?.Count}");
        _x11InkProvider.InkWindow.ModeInputDispatcher.Move(inputInfo.InputArgs);

        //canvas.Move(inputInfo);
    }

    private void InkCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        //Console.WriteLine($"InkCanvas_OnPointerReleased");
        //if (_inkInfoCache.Remove(e.Pointer.PointerId, out var inkInfo))
        //{
        //    var pointerPoint = e.GetCurrentPoint(InkCanvas);
        //    Point position = pointerPoint.Position;
        //    inkInfo.PointList.Add(position);
        //    DrawStroke(inkInfo);
        //}

        //LogTextBlock.Text += $"抬起： {e.Pointer.PointerId}\r\n";
        //LogTextBlock.Text += $"当前按下点数： {_inkInfoCache.Count} [{string.Join(',', _inkInfoCache.Keys)}]";
        InvokeAsync(canvas =>
        {
            //if (_x11InkProvider is null)
            //{
            //    return;
            //}
            canvas.ModeInputDispatcher.Up(ToModeInputArgs(e));
            //_skPathList.AddRange(canvas.CurrentInkStrokePathEnumerable);
            //canvas.Up(ToInkingInputInfo(e));

            //Console.WriteLine($"InkCanvas_OnPointerReleased InvokeAsync ModeInputDispatcher.Up");

            InvalidateToRedraw();
        });
    }

    private void InvalidateToRedraw()
    {
        SkXamlCanvas.Invalidate();
        // 对于 SkiaVisual 需要使用以下方式
        //this.InvalidateArrange();
    }

    //private readonly List<string> _skPathList = [];

    //private readonly Dictionary<uint /*PointerId*/, InkInfo> _inkInfoCache = new Dictionary<uint, InkInfo>();

    //private void DrawStroke(InkInfo inkInfo)
    //{
    //    var pointList = inkInfo.PointList;
    //    if (pointList.Count < 2)
    //    {
    //        // 小于两个点的无法应用算法
    //        return;
    //    }

    //    int inkSize = 16;

    //    var inkElement = MyInkRender.CreatePath(inkInfo, inkSize);

    //    if (inkInfo.InkElement is null)
    //    {
    //        InkCanvas.Children.Add(inkElement);
    //    }
    //    else if (inkElement != inkInfo.InkElement)
    //    {
    //        RemoveInkElement(inkInfo.InkElement);
    //        InkCanvas.Children.Add(inkElement);
    //    }

    //    inkInfo.InkElement = inkElement;
    //}

    private void RemoveInkElement(FrameworkElement? inkElement)
    {
        if (inkElement != null)
        {
            InkCanvas.Children.Remove(inkElement);
        }
    }

    private X11InkProvider? _x11InkProvider;

    //private void DrawInNative(Point position)
    //{
    //    if (OperatingSystem.IsLinux())
    //    {
    //        //_x11InkProvider!.Draw(position);
    //    }
    //}

    private Task InvokeAsync(Action<SkInkCanvas> action)
    {
        if (_x11InkProvider is null)
        {
            return Task.CompletedTask;
        }

        if (OperatingSystem.IsLinux())
        {
            // 线程调度不慢，但是线程跑满了
            //var stopwatch = Stopwatch.StartNew();
            return _x11InkProvider.InkWindow.InvokeAsync(canvas =>
            {
                //stopwatch.Stop();
                //Console.WriteLine($"线程调度耗时 {stopwatch.ElapsedMilliseconds}ms");
                action(canvas);
            });
        }

        return Task.CompletedTask;
    }

    ///// <summary>
    ///// 静态笔迹层
    ///// </summary>
    //private readonly List<StrokeCollectionInfo> _currentStaticStrokeList = new List<StrokeCollectionInfo>();

    //private async void SkiaVisual_OnDraw(object? sender, SKSurface e)
    //{
    //    using var skPaint = new SKPaint();
    //    skPaint.StrokeWidth = 0f;
    //    skPaint.IsAntialias = true;
    //    skPaint.IsStroke = false;
    //    skPaint.FilterQuality = SKFilterQuality.High;
    //    skPaint.Style = SKPaintStyle.Fill;

    //    List<StrokeCollectionInfo>? strokeCollectionInfoList;
    //    lock (_locker)
    //    {
    //        if (StrokeInfoList.Count == 0)
    //        {
    //            strokeCollectionInfoList = null;
    //        }
    //        else
    //        {
    //            strokeCollectionInfoList = [.. StrokeInfoList];
    //            StrokeInfoList.Clear();
    //        }
    //    }

    //    if (strokeCollectionInfoList != null)
    //    {
    //        _currentStaticStrokeList.AddRange(strokeCollectionInfoList);
    //    }

    //    // 由于 SkiaVisual 是清空画布渲染的，因此需要每个笔迹每次都重新渲染
    //    // 这就意味着如果笔迹数量多了，那就会卡渲染
    //    // 可选后续换成一个静态画布层优化性能
    //    foreach (var strokesCollectionInfo in _currentStaticStrokeList)
    //    {
    //        skPaint.Color = strokesCollectionInfo.StrokeColor;
    //        //skPaint.Color = SKColors.Black;
    //        var path = strokesCollectionInfo.InkStrokePath;
    //        System.Diagnostics.Debug.Assert(path != null);

    //        e.Canvas.DrawPath(path, skPaint);
    //        Console.WriteLine($"DrawPath");
    //    }

    //    if (strokeCollectionInfoList != null)
    //    {
    //        // 延迟一下，减少闪烁，确保 UNO 这一层绘制完成
    //        await Task.Delay(100);
    //        // 清空笔迹，换成在 UNO 层绘制
    //        await InvokeAsync(canvas =>
    //        {
    //            //canvas.RaiseRenderBoundsChanged(new Rect(0, 0, canvas.ApplicationDrawingSkBitmap!.Width,
    //            //    canvas.ApplicationDrawingSkBitmap.Height));
    //            canvas.CleanStroke(strokeCollectionInfoList);
    //        });
    //    }
    //}

    private async void SkXamlCanvas_OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        //Console.WriteLine($"SkXamlCanvas_OnPaintSurface");
        //return;

        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 0f;
        skPaint.IsAntialias = true;
        skPaint.IsStroke = false;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Fill;

        List<StrokeCollectionInfo> strokeCollectionInfoList;
        lock (_locker)
        {
            if (StrokeInfoList.Count == 0)
            {
                return;
            }

            strokeCollectionInfoList = [.. StrokeInfoList];
            StrokeInfoList.Clear();
        }

        //StaticDebugLogger.WriteLine($"收集笔迹数量 {strokeCollectionInfoList.Count} ");

        foreach (var strokesCollectionInfo in strokeCollectionInfoList)
        {
            skPaint.Color = strokesCollectionInfo.StrokeColor;
            //skPaint.Color = SKColors.Black;
            var path = strokesCollectionInfo.InkStrokePath;
            System.Diagnostics.Debug.Assert(path != null, "能被收集到的笔迹点一定不是空");

            e.Surface.Canvas.DrawPath(path, skPaint);
            StaticDebugLogger.WriteLine($"DrawPath");
        }

        // 界面不会自动刷新，需要等下一次才能刷新
        this.InvalidateArrange();

        //foreach (var skPath in _skPathList)
        //{
        //    Console.WriteLine($"准备到 UNO 绘制");
        //    // 需要进行序列化和反序列化是为了解决跨线程访问 SKPath 导致爆的问题
        //    // 可以切到 c82dcaf20da0948aede539b699f47926635b94a3 进行测试
        //    // 写一笔就能复现
        //    // 实际原因是 SkInkCanvas 的 InkStrokePath 在 DrawStrokeContext 的 Dispose 被释放。加上 01fd5aebad41efef3ec9afaaaefcd30a0d674cb0 即可解决，不需要序列化
        //    var path = SKPath.ParseSvgPathData(skPath);
        //    e.Surface.Canvas.DrawPath(path, skPaint);
        //}
        // 如果在主线程其他也输出控制台，也能解决  X11 的 XShapeCombineRegion 的返回 fa08b6854bd9d43445fa3d9e93cb2ebc1d4a9cca 这里的更改就是在主线程随便输出
        // 如果注释掉这句话，将不能正常完成 X11 的 XShapeCombineRegion 的返回
        StaticDebugLogger.WriteLine($"完成 UNO 绘制");

        //_skPathList.Clear();
        // 延迟一下，减少闪烁，确保 UNO 这一层绘制完成。没有找到 X11 同步渲染方法
        await Task.Delay(100);
        // 清空笔迹，换成在 UNO 层绘制
        await InvokeAsync(canvas =>
        {
            //canvas.RaiseRenderBoundsChanged(new Rect(0, 0, canvas.ApplicationDrawingSkBitmap!.Width,
            //    canvas.ApplicationDrawingSkBitmap.Height));
            canvas.CleanStroke(strokeCollectionInfoList);
        });
    }

    private void ExitProcessButton_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }

    private async void DebugButton_OnClick(object sender, RoutedEventArgs e)
    {
        await InvokeAsync(canvas =>
        {
            canvas.Debug();
        });
    }
}

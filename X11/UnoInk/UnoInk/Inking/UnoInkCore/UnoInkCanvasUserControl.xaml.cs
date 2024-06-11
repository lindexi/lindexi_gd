<<<<<<< HEAD
=======
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

>>>>>>> f682a1d5e3b6d0cb0b99903c22a9dcd7e23eb4ae
using Windows.Foundation;
<<<<<<< HEAD
using Microsoft.UI.Xaml.Input;
=======
using Windows.Foundation.Collections;
<<<<<<< HEAD
>>>>>>> d0a62b7dccda3eea79dfcc7566c8045f2f557abf
=======
using Microsoft.UI.Input;
>>>>>>> 2b488acf5567399adc4d1d22efeb2d8e9d41829e
using SkiaSharp;
using SkiaSharp.Views.Windows;
using UnoInk.Inking.InkCore;
using UnoInk.Inking.InkCore.Interactives;
using UnoInk.Inking.X11Ink;
using UnoInk.Inking.X11Platforms.Threading;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UnoInk.Inking.UnoInkCore;

public sealed partial class UnoInkCanvasUserControl : UserControl
{
    public UnoInkCanvasUserControl()
    {
        this.InitializeComponent();

        Loaded += MainPage_Loaded;
        Console.WriteLine($"主线程随便输出");
    }

    public UnoInkCanvasUserControl(Window currentWindow) : this()
    {
        _currentWindow = currentWindow;
    }

    private readonly Window? _currentWindow;

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsLinux())
        {
<<<<<<< HEAD
<<<<<<< HEAD
            //// 尝试修复 UNO 窗口不显示出来，之前在 Avalonia 也能复现
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //if (_x11InkProvider == null)
            //{
            //    _x11InkProvider = new X11InkProvider();

            //    _x11InkProvider.Start(Window.Current!);

            //    _dispatcherRequiring = new DispatcherRequiring(InvokeInk, _x11InkProvider.InkWindow.GetDispatcher());
            //}
=======
=======
            if (_currentWindow is null)
            {
                throw new InvalidOperationException($"只有传入窗口时，才能使用覆盖窗口的 X11 动态笔迹方式");
            }

>>>>>>> 19d042ae4a4903bf0cc19752f263559130eda8c6
            if (_x11InkProvider == null)
            {
<<<<<<< HEAD
                // 尝试修复 UNO 停止渲染，但也可能是 UNO 自己的坑，就是界面不显示
                // 原先在 Avalonia 也有这样的问题
                await Task.Delay(TimeSpan.FromSeconds(1));
                try
                {
                    _x11InkProvider = new X11InkProvider();
                    
                    _x11InkProvider.Start(Window.Current!);
                    
                    _dispatcherRequiring =
                        new DispatcherRequiring(InvokeInk, _x11InkProvider.InkWindow.GetDispatcher());
                    
                    //var skInkCanvas = _x11InkProvider.InkWindow.SkInkCanvas;
                    //skInkCanvas.StrokesCollected += SkInkCanvas_StrokesCollected;
                    
                    Console.WriteLine($"完成初始化");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
=======
                _x11InkProvider = new X11InkProvider();

                _x11InkProvider.Start(_currentWindow);

                _dispatcherRequiring =
                    new DispatcherRequiring(InvokeInk, _x11InkProvider.InkWindow.GetDispatcher());
                
                var skInkCanvas = _x11InkProvider.InkWindow.SkInkCanvas;
                skInkCanvas.StrokesCollected += SkInkCanvas_StrokesCollected;

<<<<<<< HEAD
                Console.WriteLine("完成初始化");
<<<<<<< HEAD
>>>>>>> a7ac643ae6800579fece2640604146dc923e2650
=======
>>>>>>> 58eb8d0baabb7bfca5d1312dbf007537a5a1c7c0
=======
                Console.WriteLine($"完成初始化");
>>>>>>> 2b488acf5567399adc4d1d22efeb2d8e9d41829e
            }
>>>>>>> f6762ea045de9b52e29ce7949a1c9be4211deaf5
        }
    }
<<<<<<< HEAD
    
    private void SkInkCanvas_StrokesCollected(object? sender, StrokesCollectionInfo e)
=======

    private void SkInkCanvas_StrokesCollected(object? sender, StrokeCollectionInfo e)
>>>>>>> 7e4dbbe7523d0540236fc7e1b7f8fb183179b7d8
    {
        Console.WriteLine($"SkInkCanvas_StrokesCollected InkId={e.InkId.Value}");

        // 这是 X11 线程进入的
        lock (StrokeInfoList)
        {
            StrokeInfoList.Add(e);
        }
    }
<<<<<<< HEAD
    
    private List<StrokesCollectionInfo> StrokeInfoList { get; } = new List<StrokesCollectionInfo>();
=======

    private List<StrokeCollectionInfo> StrokeInfoList { get; } = new List<StrokeCollectionInfo>();
<<<<<<< HEAD
>>>>>>> 7e4dbbe7523d0540236fc7e1b7f8fb183179b7d8
=======
    private readonly object _locker = new object();
>>>>>>> 2b488acf5567399adc4d1d22efeb2d8e9d41829e


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
        _firstMove = true;
        LogTextBlock.Text += $"按下： {e.Pointer.PointerId}\r\n";
        //LogTextBlock.Text += $"当前按下点数： {_inkInfoCache.Count} [{string.Join(',', _inkInfoCache.Keys)}]";
        Console.WriteLine($"按下： {e.Pointer.PointerId}");
        var inputInfo = ToModeInputArgs(e);
        // 输入时不再立刻记录，防止记录到不正确的值
        //_lastInkingInputInfo = inputInfo;
        //_dispatcherRequiring?.Require();
        InvokeAsync(canvas =>
        {
            StaticDebugLogger.WriteLine($"执行按下 {inputInfo.Position}");
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

    private bool _firstMove = true;

    class MoveInputInfo
    {
        public ModeInputArgs InputArgs { set; get; }
        public List<StylusPoint>? StylusPointList { get; set; }
    }
    private MoveInputInfo? _inputInfo;

    private void InkCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        var currentPoint = e.GetCurrentPoint(this);
        Point position = currentPoint.Position;
        LogTextBlock.Text += $"移动： {e.Pointer.PointerId} {position}\r\n";
=======
=======
=======
        if (!_isDown)
        {
            StaticDebugLogger.WriteLine($"没有按下就移动！！！InkCanvas_OnPointerMoved");
            return;
        }

>>>>>>> 90aea2d8a09e98c84ea421518827089fc2298930
        if (_firstMove)
        {
            StaticDebugLogger.WriteLine($"InkCanvas_OnPointerMoved");
        }

        _firstMove = false;

>>>>>>> df33f57b3c74a331e6651cb44dfdb4ee351f496c
        //var currentPoint = e.GetCurrentPoint(this);
        //Point position = currentPoint.Position;
>>>>>>> 0cc6a7740e41d01c0c33f9cb3960e0b79c892083

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
                Debug.Fail("进入这里时，一定存在当前的输入");
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

            SkXamlCanvas.Invalidate();
        });
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
<<<<<<< HEAD
    
    private void SkXamlCanvas_OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
=======

    private async void SkXamlCanvas_OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
>>>>>>> 634d1cb4563ff0ed67ece034dc0b98adf6f10e6d
    {
<<<<<<< HEAD
<<<<<<< HEAD
        //Console.WriteLine($"执行绘制");
        
=======
        Console.WriteLine($"SkXamlCanvas_OnPaintSurface");
=======
        //Console.WriteLine($"SkXamlCanvas_OnPaintSurface");
>>>>>>> b35874b160fd7c7597604af5567f6759edd632e4

>>>>>>> 0cc6a7740e41d01c0c33f9cb3960e0b79c892083
        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 0f;
        skPaint.IsAntialias = true;
        skPaint.IsStroke = false;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Fill;
<<<<<<< HEAD
<<<<<<< HEAD

<<<<<<< HEAD
        if (OperatingSystem.IsLinux() && _x11InkProvider?.InkWindow.SkInkCanvas.Settings.Color is { } color)
        {
            skPaint.Color = color;
        }
        else
        {
            skPaint.Color = new SKColor(0xC5, 0x20, 0x00);
        }
<<<<<<< HEAD
        
<<<<<<< HEAD
        lock (StrokeInfoList)
        {
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> 58eb8d0baabb7bfca5d1312dbf007537a5a1c7c0
            Console.WriteLine($"准备到 UNO 绘制");
            // 需要进行序列化和反序列化是为了解决跨线程访问 SKPath 导致爆的问题
            // 可以切到 c82dcaf20da0948aede539b699f47926635b94a3 进行测试
            // 写一笔就能复现
            var path = SKPath.ParseSvgPathData(skPath);
            e.Surface.Canvas.DrawPath(path, skPaint);
<<<<<<< HEAD
=======
            Console.WriteLine($"准备到 UNO 绘制 IsDispose={IsDisposed(skPath)}");
=======
            Console.WriteLine($"准备到 UNO 绘制");
>>>>>>> f7b502dbab06a8dbb1a5f6b5d98bc5459bdad98f
=======
            SKNativeObject skNativeObject = skPath;
            Console.WriteLine($"准备到 UNO 绘制 IsDispose={IsDisposed(skNativeObject)}");
>>>>>>> 625f32779848b4b1576269436c7a2a82db33449d
=======
            Console.WriteLine($"准备到 UNO 绘制");
>>>>>>> 90d5757cd64c3f92c09ebec77fc86457f75681a3
            e.Surface.Canvas.DrawPath(skPath, skPaint);
>>>>>>> f682a1d5e3b6d0cb0b99903c22a9dcd7e23eb4ae
        }
        Console.WriteLine($"完成 UNO 绘制");
=======
=======
>>>>>>> 369f36d6523bf1789b43b82d3c39e43d0b68ba96
=======

        lock (StrokeInfoList)
        {
>>>>>>> e7a4336a067f599aa6ece29c2d17b393427d2a97
            foreach (var strokesCollectionInfo in StrokeInfoList)
            {
                skPaint.Color = strokesCollectionInfo.StrokeColor;
                var path = strokesCollectionInfo.InkStrokePath;
                System.Diagnostics.Debug.Assert(path != null);
<<<<<<< HEAD
<<<<<<< HEAD
>>>>>>> 258a60849bcee8adab16c45b2303bb5f8e096058

=======
                
>>>>>>> 369f36d6523bf1789b43b82d3c39e43d0b68ba96
                e.Surface.Canvas.DrawPath(path, skPaint);
            }
            
=======
        
=======

>>>>>>> 2b488acf5567399adc4d1d22efeb2d8e9d41829e
        var strokeCollectionInfoList = new List<StrokeCollectionInfo>();
        lock (StrokeInfoList)
        {
            strokeCollectionInfoList.AddRange(StrokeInfoList);
>>>>>>> 37635096c14feec86bf6452194a3eb27faa0ece5
            StrokeInfoList.Clear();
<<<<<<< HEAD
=======
>>>>>>> 58eb8d0baabb7bfca5d1312dbf007537a5a1c7c0
=======
>>>>>>> 369f36d6523bf1789b43b82d3c39e43d0b68ba96
        }
=======
        //lock (StrokeInfoList)
        //{
        //    foreach (var strokesCollectionInfo in StrokeInfoList)
        //    {
        //        skPaint.Color = strokesCollectionInfo.StrokeColor;
        //        var path = strokesCollectionInfo.InkStrokePath;
        //        System.Diagnostics.Debug.Assert(path != null);
                
        //        e.Surface.Canvas.DrawPath(path, skPaint);
        //    }
            
        //    StrokeInfoList.Clear();
        //}
>>>>>>> 4649ce1a8be6a740ada345e8734a35759751b884
        
=======

                e.Surface.Canvas.DrawPath(path, skPaint);
            }

=======
        List<StrokeCollectionInfo> strokeCollectionInfoList;
        lock (StrokeInfoList)
        {
            strokeCollectionInfoList = [.. StrokeInfoList];
>>>>>>> ae411cdcde0e691c1346c7570f584b2776464af1
            StrokeInfoList.Clear();
        }

        //StaticDebugLogger.WriteLine($"收集笔迹数量 {strokeCollectionInfoList.Count} ");

        foreach (var strokesCollectionInfo in strokeCollectionInfoList)
        {
            skPaint.Color = strokesCollectionInfo.StrokeColor;
            skPaint.Color = SKColors.Black;
            var path = strokesCollectionInfo.InkStrokePath;
            System.Diagnostics.Debug.Assert(path != null);

            e.Surface.Canvas.DrawPath(path, skPaint);
            Console.WriteLine($"DrawPath");
        }

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD


>>>>>>> e7a4336a067f599aa6ece29c2d17b393427d2a97
=======
>>>>>>> f31f1bbc1cef90203aaa4c47f99c85c279ce2c1c
=======
        this.InvalidateViewport();
=======
        // 界面不会自动刷新，需要等下一次才能刷新
        this.InvalidateArrange();
>>>>>>> 634d1cb4563ff0ed67ece034dc0b98adf6f10e6d

>>>>>>> 0e6169f6fcc713e25c15126369a22b6b75e4e8fe
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
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        Console.WriteLine($"完成 UNO 绘制");
        
=======
        // 如果注释掉这句话，将不能正常完成 X11 的 XShapeCombineRegion 的返回
        //Console.WriteLine($"完成 UNO 绘制");
=======
        // 如果注释掉这句话，将不能正常完成 X11 的 XShapeCombineRegion 的返回
        Console.WriteLine($"完成 UNO 绘制");
>>>>>>> b1d8bf3076dc424b7970c9c49a028e95e126d8b0

>>>>>>> b5ff0f64dd3b49411632511c743bd13e2e991f37
        //_skPathList.Clear();
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        
=======

=======
        return;
>>>>>>> f3f4a61b6636dd72d83e1ee713b54042d13a1eec
        // 延迟一下，减少闪烁
=======
        // 延迟一下，减少闪烁，确保 UNO 这一层绘制完成
>>>>>>> a313c7d1fa7ffb81c04c5af29dbd36289f0f1a6d
        await Task.Delay(100);
>>>>>>> 634d1cb4563ff0ed67ece034dc0b98adf6f10e6d
        // 清空笔迹，换成在 UNO 层绘制
        await InvokeAsync(canvas =>
        {
            //canvas.RaiseRenderBoundsChanged(new Rect(0, 0, canvas.ApplicationDrawingSkBitmap!.Width,
            //    canvas.ApplicationDrawingSkBitmap.Height));
        });
=======
=======
        // 如果在主线程其他也输出控制台，也能解决  X11 的 XShapeCombineRegion 的返回 fa08b6854bd9d43445fa3d9e93cb2ebc1d4a9cca 这里的更改就是在主线程随便输出
>>>>>>> 37635096c14feec86bf6452194a3eb27faa0ece5
        // 如果注释掉这句话，将不能正常完成 X11 的 XShapeCombineRegion 的返回
        Console.WriteLine($"完成 UNO 绘制");

        //_skPathList.Clear();

<<<<<<< HEAD
<<<<<<< HEAD
=======

>>>>>>> b1d8bf3076dc424b7970c9c49a028e95e126d8b0
        //// 清空笔迹，换成在 UNO 层绘制
        //InvokeAsync(canvas =>
        //{
        //    //canvas.RaiseRenderBoundsChanged(new Rect(0, 0, canvas.ApplicationDrawingSkBitmap!.Width,
        //    //    canvas.ApplicationDrawingSkBitmap.Height));
        //});
<<<<<<< HEAD
>>>>>>> cc7dd05a09e2e5e1b7a125125e1a3bffbf4fed7c
=======
=======
>>>>>>> f31f1bbc1cef90203aaa4c47f99c85c279ce2c1c
        // 清空笔迹，换成在 UNO 层绘制
        InvokeAsync(canvas =>
        {
            //canvas.RaiseRenderBoundsChanged(new Rect(0, 0, canvas.ApplicationDrawingSkBitmap!.Width,
            //    canvas.ApplicationDrawingSkBitmap.Height));
<<<<<<< HEAD
        });
>>>>>>> 4d8052bd6f176eda3d572c8b9c96f1ce0fbd0218
=======
>>>>>>> b1d8bf3076dc424b7970c9c49a028e95e126d8b0
=======
            canvas.CleanStroke(strokeCollectionInfoList);
        });
>>>>>>> f31f1bbc1cef90203aaa4c47f99c85c279ce2c1c
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

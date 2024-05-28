using System;
using System.Collections.Generic;
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
using BujeeberehemnaNurgacolarje;
using ReewheaberekaiNayweelehe;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using UnoInk.X11Ink;
using Rect = Microsoft.Maui.Graphics.Rect;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UnoInk.UnoInkCore;

public sealed partial class UnoInkCanvasUserControl : UserControl
{
    public UnoInkCanvasUserControl()
    {
        this.InitializeComponent();
        
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsLinux())
        {
            if (_x11InkProvider == null)
            {
                _x11InkProvider = new X11InkProvider();

                _x11InkProvider.Start(Window.Current!);
            }
        }
    }

    private void InkCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        //var pointerPoint = e.GetCurrentPoint(InkCanvas);
        //Point position = pointerPoint.Position;

        //var inkInfo = new InkInfo();
        //_inkInfoCache[e.Pointer.PointerId] = inkInfo;
        //inkInfo.PointList.Add(position);
        //DrawStroke(inkInfo);
        //DrawInNative(position);

        LogTextBlock.Text += $"按下： {e.Pointer.PointerId}\r\n";
        //LogTextBlock.Text += $"当前按下点数： {_inkInfoCache.Count} [{string.Join(',', _inkInfoCache.Keys)}]";
        Console.WriteLine($"按下： {e.Pointer.PointerId}");

        InvokeAsync(canvas => canvas.Down(ToInkingInputInfo(e)));
    }

    private InkingInputInfo ToInkingInputInfo(PointerRoutedEventArgs args)
    {
        var currentPoint = args.GetCurrentPoint(this);

        var stylusPoint = new StylusPoint(currentPoint.Position.X, currentPoint.Position.Y, currentPoint.Properties.Pressure);
        return new InkingInputInfo((int) args.Pointer.PointerId, stylusPoint, currentPoint.Timestamp);
    }

    private Point _lastPoint;

    private void InkCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(this);
        Point position = currentPoint.Position;

        var length = Math.Pow(position.X - _lastPoint.X, 2) + Math.Pow(position.Y - _lastPoint.Y, 2);
        if (length < 10)
        {
            // 简单的丢点
            return;
        }

        _lastPoint = position;
        //if (_inkInfoCache.TryGetValue(e.Pointer.PointerId, out var inkInfo))
        //{
        //    var pointerPoint = e.GetCurrentPoint(InkCanvas);
        //    Point position = pointerPoint.Position;

        //    inkInfo.PointList.Add(position);
        //    //DrawStroke(inkInfo);
        //    DrawInNative(position);
        //}

        InvokeAsync(canvas => canvas.Move(ToInkingInputInfo(e)));
    }

    private void InkCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
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
            _skPathList.AddRange(canvas.CurrentInkStrokePathEnumerable);

            canvas.Up(ToInkingInputInfo(e));

            SkXamlCanvas.Invalidate();

        });
    }

    private readonly List<SKPath> _skPathList = [];

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
        if (OperatingSystem.IsLinux())
        {
            // 线程调度不慢，但是线程跑满了
            //var stopwatch = Stopwatch.StartNew();
            return _x11InkProvider!.InkWindow.InvokeAsync(canvas =>
            {
                //stopwatch.Stop();
                //Console.WriteLine($"线程调度耗时 {stopwatch.ElapsedMilliseconds}ms");
                action(canvas);
            });
        }

        return Task.CompletedTask;
    }

    private void SkXamlCanvas_OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        //Console.WriteLine($"执行绘制");

        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 0.1f;
        skPaint.IsAntialias = true;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Fill;
        skPaint.Color = new SKColor(0xC5, 0x20, 0x00);

        foreach (var skPath in _skPathList)
        {
            e.Surface.Canvas.DrawPath(skPath, skPaint);
        }

        _skPathList.Clear();

        // 清空笔迹，换成在 UNO 层绘制
        InvokeAsync(canvas =>
        {
            canvas.SkCanvas!.Clear();
            canvas.RaiseRenderBoundsChanged(new Rect(0, 0, canvas.ApplicationDrawingSkBitmap!.Width,
                canvas.ApplicationDrawingSkBitmap.Height));
        });
    }

}

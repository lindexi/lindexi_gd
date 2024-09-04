using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

using NarjejerechowainoBuwurjofear.Inking;

using UnoInk.Inking.InkCore;

namespace NarjejerechowainoBuwurjofear.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        //Loaded += MainView_Loaded;

        RootGrid.PointerPressed += RootGrid_PointerPressed;
        RootGrid.PointerMoved += RootGrid_PointerMoved;
        RootGrid.PointerReleased += RootGrid_PointerReleased;

        //Stopwatch stopwatch = Stopwatch.StartNew();
        //var count = 0;
        //RootGrid.PointerMoved += (sender, args) =>
        //{
        //    count++;

        //    if (stopwatch.Elapsed > TimeSpan.FromSeconds(1))
        //    {
        //        MessageTextBlock.Text = $"FPS: {count / stopwatch.Elapsed.TotalSeconds}";

        //        stopwatch.Restart();
        //        count = 0;
        //    }
        //};

        MessageTextBlock.Text = "Hello, Avalonia!";
    }

    //private Polyline? _polyline;
    private InkMode _inkMode = InkMode.Pen;

    private void RootGrid_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        _isDown = true;

        var inkingInputArgs = ToInkingInputArgs(e);

        if (_inkMode == InkMode.Pen)
        {
            AvaSkiaInkCanvas.WritingDown(inkingInputArgs);
        }
        else if (_inkMode == InkMode.Eraser)
        {
            AvaSkiaInkCanvas.EraserMode.EraserDown(inkingInputArgs);
            AvaSkiaInkCanvas.InvalidateVisual();
        }

        //_polyline = new Polyline
        //{
        //    Stroke = Brushes.Black,
        //    StrokeThickness = 2
        //};
        //_polyline.IsHitTestVisible = false;
        //RootGrid.Children.Add(_polyline);
    }

    private bool _isDown;

    private void RootGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        var inkingInputArgs = ToInkingInputArgs(e);
        if (_inkMode == InkMode.Pen)
        {
            AvaSkiaInkCanvas.WritingMove(inkingInputArgs);
        }
        else if(_inkMode == InkMode.Eraser)
        {
            if (e.Pointer.Type == PointerType.Mouse)
            {
                if (!_isDown)
                {
                    return;
                }
            }
            AvaSkiaInkCanvas.EraserMode.EraserMove(inkingInputArgs);
            AvaSkiaInkCanvas.InvalidateVisual();
        }

        //if (_polyline != null)
        //{
        //    var currentPoint = e.GetCurrentPoint(RootGrid);
        //    _polyline.Points.Add(currentPoint.Position);
        //}
    }

    private void RootGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDown = false;

        var inkingInputArgs = ToInkingInputArgs(e);
        if (_inkMode == InkMode.Pen)
        {
            AvaSkiaInkCanvas.WritingUp(inkingInputArgs);
        }
        else if (_inkMode == InkMode.Eraser)
        {
            //AvaSkiaInkCanvas.EraserMode.EraserUp(inkingInputArgs);
            AvaSkiaInkCanvas.InvalidateVisual();
        }

        //if (_polyline != null)
        //{
        //    RootGrid.Children.Remove(_polyline);
        //}
    }

    private InkingInputArgs ToInkingInputArgs(PointerEventArgs args)
    {
        var currentPoint = args.GetCurrentPoint(RootGrid);
        var (x, y) = currentPoint.Position;
        return new InkingInputArgs(currentPoint.Pointer.Id, new StylusPoint(x, y));
    }

    private async void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        for (int i = 0; i < int.MaxValue; i++)
        {
            await Task.Delay(100);
            AvaSkiaInkCanvas.InvalidateVisual();
        }
    }

    private void PenModeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _inkMode = InkMode.Pen;
    }

    private void EraserModeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _inkMode = InkMode.Eraser;
    }

    enum InkMode
    {
        Pen,
        Eraser
    }
}
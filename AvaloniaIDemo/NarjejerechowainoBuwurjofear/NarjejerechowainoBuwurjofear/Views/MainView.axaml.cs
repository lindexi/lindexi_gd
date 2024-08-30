using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
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
    }

    private Polyline? _polyline;


    private void RootGrid_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        //AvaSkiaInkCanvas.Down(ToInkingInputArgs(e));

        _polyline = new Polyline
        {
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        _polyline.IsHitTestVisible = false;
        RootGrid.Children.Add(_polyline);
    }

    private void RootGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        //AvaSkiaInkCanvas.Move(ToInkingInputArgs(e));

        if (_polyline != null)
        {
            var currentPoint = e.GetCurrentPoint(RootGrid);
            _polyline.Points.Add(currentPoint.Position);
        }
    }

    private void RootGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        //AvaSkiaInkCanvas.Up(ToInkingInputArgs(e));

        if (_polyline != null)
        {
            RootGrid.Children.Remove(_polyline);
        }
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
}
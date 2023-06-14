using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;


namespace FekemreakairlayHijehereci;

class Foo : FrameworkElement
{
    public Foo()
    {
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        bool colorSelect = true;
        for (int i = 0; i < 10000; i++)
        {
            var x = Random.Shared.Next((int) ActualWidth);
            var y = Random.Shared.Next((int) ActualHeight);
            drawingContext.DrawRectangle(colorSelect ? Brushes.Green : Brushes.DarkGreen, null, new Rect(x, y, 20, 20));
            colorSelect = !colorSelect;
        }
    }
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        CompositionTarget.Rendering += CompositionTarget_Rendering;
    }

    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        Foo.InvalidateVisual();
        TextBlock.Text = $"FPS: {1 / _stopwatch.Elapsed.TotalSeconds}";
        _stopwatch.Restart();
    }

    private readonly Stopwatch _stopwatch = new Stopwatch();
}

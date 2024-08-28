using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using SkiaSharp;

namespace NarjejerechowainoBuwurjofear.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        Loaded += MainView_Loaded;
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

class AvaSkiaInkCanvas : Control
{
    public override void Render(DrawingContext context)
    {
        context.Custom(new InkCanvasCustomDrawOperation(Bounds));
    }

    class InkCanvasCustomDrawOperation : ICustomDrawOperation
    {
        public InkCanvasCustomDrawOperation(Rect bounds)
        {
            Bounds = bounds;
        }

        public void Dispose()
        {

        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public bool HitTest(Point p)
        {
            return false;
        }

        public void Render(ImmediateDrawingContext context)
        {
            var skiaSharpApiLeaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (skiaSharpApiLeaseFeature == null)
            {
                return;
            }

            using var skiaSharpApiLease = skiaSharpApiLeaseFeature.Lease();
            var canvas = skiaSharpApiLease.SkCanvas;

            using var skPaint = new SKPaint();
            skPaint.Color = SKColors.Red;
            skPaint.Style = SKPaintStyle.Fill;

            var x = Random.Shared.Next(1000);
            var y = Random.Shared.Next(1000);
            canvas.DrawRect(x, y, 10, 10, skPaint);
        }

        public Rect Bounds { get; }
    }
}
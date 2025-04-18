using System.Windows;
using System.Windows.Media;
using InkBase;
using NarjejerechowainoBuwurjofear.Inking.Contexts;

namespace WpfInk;

public class WpfInkLayer : IWpfInkLayer
{
    public WpfInkLayer(WpfInkWindow inkWindow)
    {
        InkWindow = inkWindow;
    }

    public WpfInkWindow InkWindow { get; }

    public void Render(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 100));
    }

    public void Down(InkPoint screenPoint)
    {
        throw new NotImplementedException();
    }

    public void Move(InkPoint screenPoint)
    {
        throw new NotImplementedException();
    }

    public void Up(InkPoint screenPoint)
    {
        throw new NotImplementedException();
    }

    public event EventHandler<SkiaStroke>? StrokeCollected;
    public void HideStroke(SkiaStroke skiaStroke)
    {
        throw new NotImplementedException();
    }

    public SkiaStroke PointListToStroke(InkId id, IReadOnlyList<InkPoint> points)
    {
        throw new NotImplementedException();
    }
}
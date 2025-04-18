using System.Windows;
using System.Windows.Ink;
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

    private readonly Dictionary<InkId, WpfInkDrawingContext> _dictionary = [];

    public void Render(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(_isBlue ? Brushes.Blue : Brushes.Red, null, new Rect(10, 10, 100, 100));
        _isBlue = !_isBlue;
    }

    private bool _isBlue;

    public void Down(InkPoint screenPoint)
    {
        Run(() =>
        {
            var context = new WpfInkDrawingContext();
            _dictionary[screenPoint.Id] = context;
            context.PointList.Add(screenPoint);

            InkWindow.InvalidateVisual();
        });
    }

    public void Move(InkPoint screenPoint)
    {
        Run(() =>
        {
            if (_dictionary.TryGetValue(screenPoint.Id, out var context))
            {
                context.PointList.Add(screenPoint);
                
                InkWindow.InvalidateVisual();
            }
        });
    }

    public void Up(InkPoint screenPoint)
    {
        Run(() =>
        {
            if (_dictionary.TryGetValue(screenPoint.Id, out var context))
            {
                context.PointList.Add(screenPoint);

                InkWindow.InvalidateVisual();
            }
        });
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

    private void Run(Action action)
    {
        InkWindow.Dispatcher.InvokeAsync(action);
    }
}

class WpfInkDrawingContext
{
    public List<InkPoint> PointList { get; } = [];

    //public Stroke Stroke { get; } 
}
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
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

    public StandardRgbColor Color { set; get; } = StandardRgbColor.Red;
    public double InkThickness { set; get; } = 6;

    public void Render(DrawingContext drawingContext)
    {
        //drawingContext.DrawRectangle(_isBlue ? Brushes.Blue : Brushes.Red, null, new Rect(10, 10, 100, 100));
        _isBlue = !_isBlue;

        foreach (WpfInkDrawingContext context in _dictionary.Values)
        {
            if (context.IsHide)
            {
                continue;
            }

            var stroke = context.Stroke;
            var geometry = stroke.GetGeometry();
            var brush = new SolidColorBrush(context.DrawingAttributes.Color);
            drawingContext.DrawGeometry(brush, null, geometry);
        }
    }

    private bool _isBlue;

    public void Down(InkPoint screenPoint)
    {
        Run(() =>
        {
            var drawingAttributes = new DrawingAttributes()
            {
                Color = Color.ToWpfColor(),
                Width = InkThickness,
                Height = InkThickness,
            };
            drawingAttributes.FitToCurve = true;

            var context = new WpfInkDrawingContext(drawingAttributes);
            _dictionary[screenPoint.Id] = context;
            context.Add(screenPoint);

            InkWindow.InvalidateVisual();
        });
    }

    public void Move(InkPoint screenPoint)
    {
        Run(() =>
        {
            if (_dictionary.TryGetValue(screenPoint.Id, out var context))
            {
                context.Add(screenPoint);

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
                context.Add(screenPoint);

                InkWindow.InvalidateVisual();
            }
        });
    }

    public event EventHandler<SkiaStroke>? StrokeCollected;
    public void HideStroke(SkiaStroke skiaStroke)
    {
        Run(() =>
        {
            if (_dictionary.TryGetValue(skiaStroke.Id, out var context))
            {
                context.IsHide = !context.IsHide;
            }

            InkWindow.InvalidateVisual();
        });
    }

    public void ToggleShowHideAllStroke()
    {
        Run(() =>
        {
            foreach (var context in _dictionary.Values)
            {
                context.IsHide = !context.IsHide;
            }

            InkWindow.InvalidateVisual();
        });
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
    public WpfInkDrawingContext(DrawingAttributes drawingAttributes)
    {
        DrawingAttributes = drawingAttributes;
    }

    public bool IsHide { get; set; }

    public DrawingAttributes DrawingAttributes { get; }

    public List<InkPoint> PointList { get; } = [];

    public Stroke Stroke
    {
        get
        {
            if (_stroke == null)
            {
                _stroke = new Stroke(StylusPointCollection, DrawingAttributes);
            }

            return _stroke;
        }
    }

    private Stroke? _stroke;

    private StylusPointCollection StylusPointCollection { get; } = new StylusPointCollection();

    public void Add(InkPoint point)
    {
        PointList.Add(point);

        StylusPointCollection.Add(new StylusPoint(point.X, point.Y, point.PressureFactor));

        _stroke = null;
    }
}
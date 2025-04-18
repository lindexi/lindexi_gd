using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

using InkBase;

using NarjejerechowainoBuwurjofear.Inking.Contexts;
using SkiaSharp;

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
            var context = new WpfInkDrawingContext(Color,InkThickness);
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

                var geometry = context.Stroke.GetGeometry();
                var path = geometry.ToString();
                if (path.StartsWith("F1"))
                {
                    path = path.Substring("F1".Length);
                }
                var skPath = SKPath.ParseSvgPathData(path);
                StrokeCollected?.Invoke(this, new SkiaStroke(screenPoint.Id, skPath)
                {
                    Color = context.Color,
                    PointList = context.PointList,
                });
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
    public WpfInkDrawingContext(StandardRgbColor color, double inkThickness)
    {
        var drawingAttributes = new DrawingAttributes()
        {
            Color = color.ToWpfColor(),
            Width = inkThickness,
            Height = inkThickness,
        };
        drawingAttributes.FitToCurve = true;
        DrawingAttributes = drawingAttributes;

        Color = color;
        InkThickness = inkThickness;
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
    public StandardRgbColor Color { get; set; }
    public double InkThickness { get; set; }

    public void Add(InkPoint point)
    {
        PointList.Add(point);

        StylusPointCollection.Add(new StylusPoint(point.X, point.Y, point.PressureFactor));

        _stroke = null;
    }
}
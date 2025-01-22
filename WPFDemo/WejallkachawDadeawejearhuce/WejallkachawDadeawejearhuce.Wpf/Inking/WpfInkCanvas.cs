using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using UnoInk.Inking.InkCore;

using WejallkachawDadeawejearhuce.Inking.Contexts;
using WejallkachawDadeawejearhuce.Inking.WpfInking;

using Point = Microsoft.Maui.Graphics.Point;

namespace WejallkachawDadeawejearhuce.Wpf.Inking;

public class WpfInkCanvas : FrameworkElement, IWpfInkCanvas
{
    public WpfInkCanvas()
    {
        IsHitTestVisible = false;
    }

    void IWpfInkCanvas.WritingDown(InkingInputArgs inkingInputArgs)
    {
        Dispatcher.Invoke(() => Down(inkingInputArgs), DispatcherPriority.Send);
    }

    void IWpfInkCanvas.WritingMove(InkingInputArgs inkingInputArgs)
    {
        Dispatcher.Invoke(() => Move(inkingInputArgs), DispatcherPriority.Send);
    }

    void IWpfInkCanvas.WritingUp(InkingInputArgs inkingInputArgs)
    {
        Dispatcher.Invoke(() => Up(inkingInputArgs), DispatcherPriority.Send);
    }

    private InkingInputArgs Transform(in InkingInputArgs inkingInputArgs)
    {
        var pointFromScreen = PointFromScreen(new System.Windows.Point(0, 0));
        var point = inkingInputArgs.Point.Point;
        point = point with
        {
            X = point.X + pointFromScreen.X,
            Y = point.Y + pointFromScreen.Y
        };

        return inkingInputArgs with
        {
            Point = inkingInputArgs.Point with
            {
                Point = point
            }
        };
    }

    public void Down(InkingInputArgs inkingInputArgs)
    {
        inkingInputArgs = Transform(in inkingInputArgs);
        _contextDictionary[inkingInputArgs.Id] = new DynamicStrokeContext();

        _contextDictionary[inkingInputArgs.Id].StylusPointQueue.Enqueue(inkingInputArgs.Point);
        InvalidateVisual();
    }

    public void Move(InkingInputArgs inkingInputArgs)
    {
        if (_contextDictionary.TryGetValue(inkingInputArgs.Id, out var context))
        {
            inkingInputArgs = Transform(in inkingInputArgs);

            context.StylusPointQueue.Enqueue(inkingInputArgs.Point);
            InvalidateVisual();
        }
    }

    public void Up(InkingInputArgs inkingInputArgs)
    {
        if (_contextDictionary.TryGetValue(inkingInputArgs.Id, out var context))
        {
            inkingInputArgs = Transform(in inkingInputArgs);
            context.StylusPointQueue.Enqueue(inkingInputArgs.Point);
            InvalidateVisual();
        }
    }

    protected override GeometryHitTestResult? HitTestCore(GeometryHitTestParameters hitTestParameters)
    {
        return null;
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return null;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        foreach (var context in _contextDictionary.Values)
        {
            if (context.StylusPointQueue.Count < 2)
            {
                continue;
            }

            context.StreamGeometry.FillRule = FillRule.Nonzero;
            context.StreamGeometry.Clear();

            StreamGeometryContext streamGeometryContext = context.StreamGeometry.Open();
            Point[] outlinePointList = SimpleInkRender.GetOutlinePointList(context.StylusPointQueue, 20);

            streamGeometryContext.BeginFigure(outlinePointList[0].ToWpfPoint(), true, true);
            streamGeometryContext.PolyLineTo(outlinePointList.Select(t => t.ToWpfPoint()).ToList(), false, false);
            streamGeometryContext.Close();

            drawingContext.DrawGeometry(_brush, null, context.StreamGeometry);
        }
    }

    private readonly SolidColorBrush _brush = new SolidColorBrush(Colors.Blue)
    {
        Opacity = 0.6
    };

    private readonly Dictionary<int, DynamicStrokeContext> _contextDictionary = [];
}
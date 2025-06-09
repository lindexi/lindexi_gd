extern alias WpfInk;
using System.Windows;
using System.Windows.Media;

using WpfApp.InkDataModels;

using WpfInk::MS.Internal.Ink;
using WpfInk::System.Windows.Ink;
using WpfInk::System.Windows.Input;

namespace WpfApp.Inking;

public class SimpleInkCanvas : FrameworkElement
{
    public SimpleInkCanvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var arrayOfArrayOfInkDataModel = ArrayOfArrayOfInkDataModel.ReadFromFile(@"Assets\Ink.xml");
        foreach (ArrayOfInkDataModel arrayOfInkDataModel in arrayOfArrayOfInkDataModel)
        {
            //if (arrayOfInkDataModel.Count < 10)
            //{
            //    continue;
            //}

            var streamGeometry = new StreamGeometry()
            {
                FillRule = FillRule.Nonzero
            };
            using var streamGeometryContext = streamGeometry.Open();
            WpfInk::WpfInk.API.IStreamGeometryContext context = new InkingStreamGeometryContext(streamGeometryContext);

            var drawingAttributes = new WpfInk::System.Windows.Ink.DrawingAttributes()
            {
                Width = 10,
                Height = 10,
            };
            var stylusPointCollection = new StylusPointCollection();
            foreach (var inkDataModel in arrayOfInkDataModel)
            {
                stylusPointCollection.Add(new WpfInk::System.Windows.Input.StylusPoint(inkDataModel.X, inkDataModel.Y));
            }

            WpfInk::System.Windows.Ink.Stroke stroke = new WpfInk::System.Windows.Ink.Stroke(stylusPointCollection, drawingAttributes);
            var strokeNodeIterator = StrokeNodeIterator.GetIterator(stroke, drawingAttributes);
            StrokeRenderer.CalcGeometryAndBounds(strokeNodeIterator, drawingAttributes, calculateBounds: false, context, out _);

            drawingContext.DrawGeometry(Brushes.Red, null, streamGeometry);
        }
    }
}

class InkingStreamGeometryContext : WpfInk::WpfInk.API.IStreamGeometryContext
{
    public InkingStreamGeometryContext(StreamGeometryContext streamGeometryContext)
    {
        StreamGeometryContext = streamGeometryContext;
    }

    private StreamGeometryContext StreamGeometryContext { get; }

    public void BeginFigure(WpfInk::WpfInk.PresentationCore.System.Windows.Point startPoint, bool isFilled, bool isClosed)
    {
        StreamGeometryContext.BeginFigure(startPoint.ToPoint(), isFilled, isClosed);
    }

    public void PolyBezierTo(IList<WpfInk::WpfInk.PresentationCore.System.Windows.Point> points, bool isStroked, bool isSmoothJoin)
    {
        StreamGeometryContext.PolyBezierTo(points.Select(t => t.ToPoint()).ToList(), isStroked, isSmoothJoin);
    }

    public void PolyLineTo(IList<WpfInk::WpfInk.PresentationCore.System.Windows.Point> points, bool isStroked, bool isSmoothJoin)
    {
        StreamGeometryContext.PolyLineTo(points.Select(t => t.ToPoint()).ToList(), isStroked, isSmoothJoin);
    }

    public void ArcTo(WpfInk::WpfInk.PresentationCore.System.Windows.Point point, WpfInk::WpfInk.PresentationCore.System.Windows.Size size, double rotationAngle, bool isLargeArc, bool sweepDirection, bool isStroked,
        bool isSmoothJoin)
    {
        StreamGeometryContext.ArcTo(point.ToPoint(), size.ToSize(), rotationAngle, isLargeArc, sweepDirection ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, isStroked, isSmoothJoin);
    }
}
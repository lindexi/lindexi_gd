extern alias WpfInk;
using System.Windows;
using System.Windows.Media;

using SkiaInk;

using SkiaSharp;

using WpfApp.InkDataModels;

using WpfInk::MS.Internal.Ink;
using WpfInk::System.Windows.Ink;
using WpfInk::System.Windows.Input;
using WpfInk::WpfInk.API;

namespace WpfApp.Inking;

public class SimpleInkCanvas : FrameworkElement
{
    public SimpleInkCanvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        MouseMove += SimpleInkCanvas_MouseMove;
    }

    public SkiaCanvas? SkiaCanvas { get; set; }

    private void SimpleInkCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        Point point = e.GetPosition(this);
        PointList.Add((point.X, point.Y));
        InvalidateVisual();
    }

    private List<(double X, double Y)> PointList { get; } = [];

    protected override GeometryHitTestResult? HitTestCore(GeometryHitTestParameters hitTestParameters)
    {
        return new GeometryHitTestResult(this, IntersectionDetail.FullyContains);
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var pathList = new List<SKPath>();

        var arrayOfArrayOfInkDataModel = ArrayOfArrayOfInkDataModel.ReadFromFile(@"Assets\Ink.xml");
        foreach (ArrayOfInkDataModel arrayOfInkDataModel in arrayOfArrayOfInkDataModel)
        {
            if (arrayOfInkDataModel.Count < 20)
            {
                continue;
            }

            var stylusPointCollection = new StylusPointCollection();
            foreach (var inkDataModel in arrayOfInkDataModel)
            {
                stylusPointCollection.Add(new WpfInk::System.Windows.Input.StylusPoint(inkDataModel.X, inkDataModel.Y));
            }

            RenderStroke(stylusPointCollection);

            break;
        }

        if (PointList.Count > 2)
        {
            var stylusPointCollection = new StylusPointCollection();
            foreach (var (x, y) in PointList)
            {
                stylusPointCollection.Add(new WpfInk::System.Windows.Input.StylusPoint(x, y));
            }

            RenderStroke(stylusPointCollection);
        }

        SkiaCanvas?.Draw(canvas =>
        {
            using var skPaint = new SKPaint();
            skPaint.Color = SKColors.Blue;
            skPaint.Style = SKPaintStyle.Fill;
            skPaint.IsAntialias = true;

            foreach (var skPath in pathList)
            {
                canvas.DrawPath(skPath, skPaint);
                skPath.Dispose();
            }

            pathList.Clear();
        });


        void RenderStroke(WpfInk::System.Windows.Input.StylusPointCollection stylusPointCollection)
        {
            var streamGeometry = new StreamGeometry()
            {
                FillRule = FillRule.Nonzero
            };
            using (var streamGeometryContext = streamGeometry.Open())
            {
                WpfInk::WpfInk.API.IStreamGeometryContext context =
                    new InkingStreamGeometryContext(streamGeometryContext);

                RenderToGeometry(stylusPointCollection, context);
            }

            drawingContext.PushOpacity(0.3);
            drawingContext.DrawGeometry(Brushes.Red, null, streamGeometry);
            drawingContext.Pop();

            var text = streamGeometry.ToString();
            _ = text;

            var skPath = new SKPath();
            var skiaStreamGeometryContext = new SkiaStreamGeometryContext(skPath);
            RenderToGeometry(stylusPointCollection, skiaStreamGeometryContext);
            pathList.Add(skPath);
            var svgPathData = skPath.ToSvgPathData();
            _ = svgPathData;
            if (text.Contains(svgPathData))
            {

            }
        }
    }

    private static void RenderToGeometry(StylusPointCollection stylusPointCollection, IStreamGeometryContext context)
    {
        var drawingAttributes = new WpfInk::System.Windows.Ink.DrawingAttributes()
        {
            Width = 10,
            Height = 10,
            //StylusTip = StylusTip.Rectangle,
            //FitToCurve = true,
        };

        WpfInk::System.Windows.Ink.Stroke stroke =
            new WpfInk::System.Windows.Ink.Stroke(stylusPointCollection, drawingAttributes);
        var strokeNodeIterator = StrokeNodeIterator.GetIterator(stroke, drawingAttributes);
        StrokeRenderer.CalcGeometryAndBounds(strokeNodeIterator, drawingAttributes, calculateBounds: false,
            context, out _);
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
extern alias WpfInk;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using SkiaInk;

using SkiaSharp;

using WpfApp.InkDataModels;

using WpfInk::MS.Internal.Ink;
using WpfInk::WpfInk;
using WpfInk::WpfInk.PresentationCore.System.Windows.Ink;
using WpfInk::WpfInk.PresentationCore.System.Windows.Input.Stylus;

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

            var list = new List<InkStylusPoint2D>();

            foreach (var inkDataModel in arrayOfInkDataModel)
            {
                list.Add(new InkStylusPoint2D(inkDataModel.X, inkDataModel.Y));
            }

            RenderStroke(list);

            break;
        }

        if (PointList.Count > 2)
        {
            var list = new List<InkStylusPoint2D>();
            foreach (var (x, y) in PointList)
            {
                list.Add(new InkStylusPoint2D(x, y));
            }

            RenderStroke(list);
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


        void RenderStroke(List<InkStylusPoint2D> stylusPointCollection)
        {
            var streamGeometry = new StreamGeometry()
            {
                FillRule = FillRule.Nonzero
            };
            using (var streamGeometryContext = streamGeometry.Open())
            {
                var context =
                    new InkingStreamGeometryContext(streamGeometryContext);

                RenderToGeometry(stylusPointCollection, context);
            }

            //drawingContext.PushOpacity(0.3);
            //drawingContext.DrawGeometry(Brushes.Red, null, streamGeometry);
            //drawingContext.Pop();

            //var text = streamGeometry.ToString();
            //_ = text;

            var skPath = new SKPath();
            var skiaStreamGeometryContext = new SkiaStreamGeometryContext(skPath);
            RenderToGeometry(stylusPointCollection, skiaStreamGeometryContext);
            pathList.Add(skPath);
            //var svgPathData = skPath.ToSvgPathData();
            //_ = svgPathData;
            //if (text.Contains(svgPathData))
            //{

            //}
        }
    }

    private static void RenderToGeometry(List<InkStylusPoint2D> stylusPointCollection, IStreamGeometryContext context)
    {
       InkStrokeRenderer.Render(context,new StrokeRendererInfo()
       {
           StylusPointCollection = stylusPointCollection,
           Width = 10,
           Height = 10,
       });
    }
}
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using SkiaInk;

using SkiaSharp;

using WpfApp.InkDataModels;

using WpfInk;

namespace WpfApp.Inking;

public class SimpleInkCanvas : FrameworkElement
{
    public SimpleInkCanvas()
    {
        var socketsHttpHandler = new SocketsHttpHandler();
        socketsHttpHandler.ConnectCallback = async (context, cancellationToken) =>
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.NoDelay = true;

                if (context.DnsEndPoint.AddressFamily == AddressFamily.InterNetwork)
                {
                    // it is the ipv4
                }
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                socket.Dispose();
                throw;
            }

            return new NetworkStream(socket, ownsSocket: true);
        };

        var httpClient = new HttpClient(socketsHttpHandler);

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        MouseMove += SimpleInkCanvas_MouseMove;

        var arrayOfArrayOfInkDataModel = ArrayOfArrayOfInkDataModel.ReadFromFile(@"Assets\Ink.xml");
        _arrayOfArrayOfInkDataModel = arrayOfArrayOfInkDataModel;
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

    private readonly List<InkStylusPoint2D> _cacheList = new List<InkStylusPoint2D>();
    private readonly ArrayOfArrayOfInkDataModel _arrayOfArrayOfInkDataModel;

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var pathList = new List<SKPath>();
       
        foreach (ArrayOfInkDataModel arrayOfInkDataModel in _arrayOfArrayOfInkDataModel)
        {
            if (arrayOfInkDataModel.Count < 20)
            {
                continue;
            }

            _cacheList.Clear();
            var list = _cacheList;

            foreach (var inkDataModel in arrayOfInkDataModel)
            {
                list.Add(new InkStylusPoint2D(inkDataModel.X, inkDataModel.Y));
            }

            RenderStroke(list);

            break;
        }

        if (PointList.Count > 2)
        {
            _cacheList.Clear();
            var list = _cacheList;

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
        InkStrokeRenderer.Render(context, new StrokeRendererInfo()
        {
            StylusPointCollection = stylusPointCollection,
            Width = 10,
            Height = 10,
        });
    }
}
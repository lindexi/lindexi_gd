using System.Linq;

using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using NarjejerechowainoBuwurjofear.Inking.Contexts;
using NarjejerechowainoBuwurjofear.Inking.Utils;

using SkiaSharp;

namespace NarjejerechowainoBuwurjofear.Inking.Erasing;

public class AvaSkiaInkCanvasEraserMode
{
    public AvaSkiaInkCanvasEraserMode(AvaSkiaInkCanvas inkCanvas)
    {
        InkCanvas = inkCanvas;
    }

    public AvaSkiaInkCanvas InkCanvas { get; }
    public bool IsErasing { get; private set; }
    private int MainEraserInputId { set; get; }

    private PointPathEraserManager PointPathEraserManager { get; } = new PointPathEraserManager();

    public void StartEraser()
    {
        var staticStrokeList = InkCanvas.StaticStrokeList;
        PointPathEraserManager.StartEraserPointPath(staticStrokeList);
    }

    public void EraserDown(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (!IsErasing)
        {
            MainEraserInputId = args.Id;

            IsErasing = true;

            StartEraser();
        }
        else
        {
            // 忽略其他的输入点
        }
    }

    public void EraserMove(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (IsErasing && args.Id == MainEraserInputId)
        {
            // 擦除
            var eraserWidth = 50d;
            var eraserHeight = 70d;

            var rect = new Rect(args.Point.Point.X - eraserWidth / 2, args.Point.Point.Y - eraserHeight / 2, eraserWidth, eraserHeight);

            PointPathEraserManager.Move(rect.ToMauiRect());
        }
    }

    public void EraserUp(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (IsErasing && args.Id == MainEraserInputId)
        {
            IsErasing = false;
            var pointPathEraserResult = PointPathEraserManager.Finish();

            InkCanvas.ResetStaticStrokeListEraserResult(pointPathEraserResult.ErasingSkiaStrokeList.SelectMany(t => t.NewStrokeList));

            ErasingCompleted?.Invoke(this, new ErasingCompletedEventArgs(pointPathEraserResult.ErasingSkiaStrokeList));
        }
    }

    public event EventHandler<ErasingCompletedEventArgs>? ErasingCompleted;

    public void Render(DrawingContext context)
    {
        context.Custom(new EraserModeCustomDrawOperation(this));
    }

    class EraserModeCustomDrawOperation : ICustomDrawOperation
    {
        public EraserModeCustomDrawOperation(AvaSkiaInkCanvasEraserMode eraserMode)
        {
            var pointPathEraserManager = eraserMode.PointPathEraserManager;
            IReadOnlyList<SkiaStrokeDrawContext> drawContextList = pointPathEraserManager.GetDrawContextList();
            DrawContextList = drawContextList;

            if (drawContextList.Count == 0)
            {
                Bounds = new Rect(0, 0, 0, 0);
            }
            else
            {
                Rect bounds = drawContextList[0].DrawBounds;

                for (var i = 1; i < drawContextList.Count; i++)
                {
                    bounds = bounds.Union(drawContextList[i].DrawBounds);
                }

                Bounds = bounds;
            }
        }

        private IReadOnlyList<SkiaStrokeDrawContext> DrawContextList { get; }

        public void Dispose()
        {
            foreach (var skiaStrokeDrawContext in DrawContextList)
            {
                skiaStrokeDrawContext.Dispose();
            }
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

            skPaint.IsAntialias = true;

            skPaint.StrokeWidth = 10;

            foreach (var drawContext in DrawContextList)
            {
                // 绘制
                skPaint.Color = drawContext.Color;
                canvas.DrawPath(drawContext.Path, skPaint);
            }
        }

        public Avalonia.Rect Bounds { get; }
    }
}
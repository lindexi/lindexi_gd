using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using Microsoft.Maui.Graphics;

using NarjejerechowainoBuwurjofear.Inking.Contexts;
using NarjejerechowainoBuwurjofear.Inking.Utils;

using SkiaSharp;

using Color = Avalonia.Media.Color;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using Point = Avalonia.Point;
using Rect = Avalonia.Rect;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;

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

class EraserView : Control
{
    public EraserView()
    {
        Path1 = Geometry.Parse("M0,5.0093855C0,2.24277828,2.2303666,0,5.00443555,0L24.9955644,0C27.7594379,0,30,2.23861485,30,4.99982044L30,17.9121669C30,20.6734914,30,25.1514578,30,27.9102984L30,40.0016889C30,42.7621799,27.7696334,45,24.9955644,45L5.00443555,45C2.24056212,45,0,42.768443,0,39.9906145L0,5.0093855z");
        //skPaint.Color = new SKColor(0, 0, 0, 0x33);
        Path1FillBrush = new SolidColorBrush(new Color(0x33, 0, 0, 0));

        Path2 = Geometry.Parse("M20,29.1666667L20,16.1666667C20,15.3382395 19.3284271,14.6666667 18.5,14.6666667 17.6715729,14.6666667 17,15.3382395 17,16.1666667L17,29.1666667C17,29.9950938 17.6715729,30.6666667 18.5,30.6666667 19.3284271,30.6666667 20,29.9950938 20,29.1666667z M13,29.1666667L13,16.1666667C13,15.3382395 12.3284271,14.6666667 11.5,14.6666667 10.6715729,14.6666667 10,15.3382395 10,16.1666667L10,29.1666667C10,29.9950938 10.6715729,30.6666667 11.5,30.6666667 12.3284271,30.6666667 13,29.9950938 13,29.1666667z");
        Path2FillBrush = new SolidColorBrush(new Color(0x26, 0, 0, 0));

        Path3FillBrush = new SolidColorBrush(new Color(0xFF, 0xF2, 0xEE, 0xEB));

        var bounds = Path1.Bounds.Union(Path2.Bounds);
        Width = bounds.Width;
        Height = bounds.Height;

        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        IsHitTestVisible = false;
    }

    private Geometry Path1 { get; }
    private IBrush Path1FillBrush { get; }

    private Geometry Path2 { get; }
    private IBrush Path2FillBrush { get; }

    private IBrush Path3FillBrush { get; }

    public override void Render(DrawingContext context)
    {
        context.DrawGeometry(Path1FillBrush, null, Path1);
        //skPaint.Color = new SKColor(0xF2, 0xEE, 0xEB, 0xFF);
        //skCanvas.DrawRoundRect(1, 1, 28, 43, 4, 4, skPaint);
        context.DrawRectangle(Path3FillBrush, null, new RoundedRect(new Rect(1, 1, 28, 43), 4));
        context.DrawGeometry(Path2FillBrush, null, Path2);
    }
}
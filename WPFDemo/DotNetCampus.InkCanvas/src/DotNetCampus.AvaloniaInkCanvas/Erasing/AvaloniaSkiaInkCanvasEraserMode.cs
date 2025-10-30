using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using DotNetCampus.Inking.Contexts;
using DotNetCampus.Inking.Utils;
using SkiaSharp;
using UnoInk.Inking.InkCore.Interactives;
//using Microsoft.Maui.Graphics;
using Point = Avalonia.Point;
using Rect = Avalonia.Rect;
using Size = Avalonia.Size;

namespace DotNetCampus.Inking.Erasing;

public class AvaloniaSkiaInkCanvasEraserMode
{
    public AvaloniaSkiaInkCanvasEraserMode(AvaloniaSkiaInkCanvas inkCanvas)
    {
        InkCanvas = inkCanvas;
    }

    private void InkCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _debugEraserSizeScale += e.Delta.Y;
    }

    private double _debugEraserSizeScale = 0;

    public AvaloniaSkiaInkCanvas InkCanvas { get; }
    public bool IsErasing { get; private set; }
    private int MainEraserInputId { set; get; }

    private PointPathEraserManager PointPathEraserManager { get; } = new PointPathEraserManager();

    private readonly EraserView _eraserView = new EraserView();

    public void StartEraser()
    {

#if DEBUG
        var topLevel = TopLevel.GetTopLevel(InkCanvas)!;
        topLevel.PointerWheelChanged -= InkCanvas_PointerWheelChanged;
        topLevel.PointerWheelChanged += InkCanvas_PointerWheelChanged;
#endif

        var staticStrokeList = InkCanvas.StaticStrokeList;
        PointPathEraserManager.StartEraserPointPath(staticStrokeList);

        InkCanvas.AddChild(_eraserView);
    }

    public void EraserDown(InkingModeInputArgs args)
    {
        //InkCanvas.EnsureInputConflicts();
        if (!IsErasing)
        {
            MainEraserInputId = args.Id;

            IsErasing = true;

            StartEraser();

            _eraserView.Move(args.Position.ToAvaloniaPoint());
        }
        else
        {
            // 忽略其他的输入点
        }
    }

    public void EraserMove(InkingModeInputArgs args)
    {
        //InkCanvas.EnsureInputConflicts();
        if (IsErasing && args.Id == MainEraserInputId)
        {
            // 擦除
            var eraserWidth = 50d;
            var eraserHeight = 70d;

#if DEBUG
            if (_debugEraserSizeScale > 0)
            {
                _debugEraserSizeScale = Math.Min(100, _debugEraserSizeScale);

                eraserWidth *= (1 + _debugEraserSizeScale / 10);
                eraserHeight *= (1 + _debugEraserSizeScale / 10);
            }
            else if (_debugEraserSizeScale < -10)
            {
                _debugEraserSizeScale = Math.Max(-100, _debugEraserSizeScale);

                eraserWidth *= (1 + _debugEraserSizeScale / 100);
                eraserHeight *= (1 + _debugEraserSizeScale / 100);
            }
#endif

            var rect = new Rect(args.Position.X - eraserWidth / 2, args.Position.Y - eraserHeight / 2, eraserWidth, eraserHeight);
            PointPathEraserManager.Move(rect.ToRect2D());

            _eraserView.SetEraserSize(new Size(eraserWidth, eraserHeight));
            _eraserView.Move(args.Position.ToAvaloniaPoint());
        }
    }

    public void EraserUp(InkingModeInputArgs args)
    {
        //InkCanvas.EnsureInputConflicts();
        if (IsErasing && args.Id == MainEraserInputId)
        {
            IsErasing = false;
            var pointPathEraserResult = PointPathEraserManager.Finish();

            InkCanvas.ResetStaticStrokeListByEraserResult(pointPathEraserResult.ErasingSkiaStrokeList.SelectMany(t => t.NewStrokeList));

            ClearEraser();

            ErasingCompleted?.Invoke(this, new ErasingCompletedEventArgs(pointPathEraserResult.ErasingSkiaStrokeList));
        }
    }

    private void ClearEraser()
    {
        InkCanvas.RemoveChild(_eraserView);
    }

    public event EventHandler<ErasingCompletedEventArgs>? ErasingCompleted;

    public void Render(DrawingContext context)
    {
        context.Custom(new EraserModeCustomDrawOperation(this));
    }

    class EraserModeCustomDrawOperation : ICustomDrawOperation
    {
        public EraserModeCustomDrawOperation(AvaloniaSkiaInkCanvasEraserMode eraserMode)
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

        public Rect Bounds { get; }
    }
}
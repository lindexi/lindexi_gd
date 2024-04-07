#nullable enable
using BujeeberehemnaNurgacolarje;

using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace ReewheaberekaiNayweelehe;

class SkInkCanvas
{
    public void SetCanvas(SKCanvas canvas)
    {
        _skCanvas = canvas;
    }
    private SKCanvas? _skCanvas;

    public SKBitmap? SkBitmap { set; get; }

    //public SKSurface? SkSurface { set; get; }

    public void Move(Point point)
    {
        var x = point.X;
        var y = point.Y;
        var currentStylusPoint = new StylusPoint(x, y);

        DrawStroke(currentStylusPoint, out _);
    }

    private bool CanDropLastPoint(Span<StylusPoint> pointList, StylusPoint currentStylusPoint)
    {
        if (pointList.Length < 2)
        {
            return false;
        }

        var lastPoint = pointList[^1];

        if (Math.Pow(lastPoint.Point.X - currentStylusPoint.Point.X, 2) + Math.Pow(lastPoint.Point.Y - currentStylusPoint.Point.Y, 2) < 100)
        {
            return true;
        }

        return false;
    }

    private int DropPointCount { set; get; }

    private bool DrawStroke(StylusPoint currentStylusPoint, out Rect drawRect)
    {
        drawRect = Rect.Zero;
        if (_stylusPoints.Count == 0)
        {
            _stylusPoints.Enqueue(currentStylusPoint);

            return false;
        }

        if (_skCanvas is null)
        {
            return false;
        }

        //if (SkSurface is null)
        //{
        //    return false;
        //}

        if (SkBitmap is null)
        {
            return false;
        }

        _stylusPoints.CopyTo(_cache, 0);
        if (CanDropLastPoint(_cache.AsSpan(0, _stylusPoints.Count), currentStylusPoint) && DropPointCount < 3)
        {
            // 丢点是为了让 SimpleInkRender 可以绘制更加平滑的折线。但是不能丢太多的点，否则将导致看起来断线
            DropPointCount++;
            return false;
        }

        DropPointCount = 0;

        var lastPoint = _cache[_stylusPoints.Count - 1];
        if (currentStylusPoint == lastPoint)
        {
            return false;
        }

        _cache[_stylusPoints.Count] = currentStylusPoint;
        _stylusPoints.Enqueue(currentStylusPoint);

        Console.WriteLine($"Count={_stylusPoints.Count}");

        for (int i = 0; i < 10; i++)
        {
            if (_stylusPoints.Count - i - 1 < 0)
            {
                break;
            }

            _cache[_stylusPoints.Count - i - 1] = _cache[_stylusPoints.Count - i - 1] with
            {
                Pressure = Math.Max(Math.Min(0.1f * i, 0.5f), 0.01f)
                //Pressure = 0.3f,
            };
        }

        var pointList = _cache.AsSpan(0, _stylusPoints.Count);

        var outlinePointList = SimpleInkRender.GetOutlinePointList(pointList, 20);
        _outlinePointList = outlinePointList;

        using var skPath = new SKPath();
        skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
        //skPath.Close();

        var skPathBounds = skPath.Bounds;

        var additionSize = 10;
        drawRect = new Rect(skPathBounds.Left - additionSize, skPathBounds.Top - additionSize, skPathBounds.Width + additionSize * 2, skPathBounds.Height + additionSize * 2);

        var skCanvas = _skCanvas;
        //skCanvas.Clear(SKColors.Transparent);
        //skCanvas.Translate(-minX,-minY);
        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 0.1f;
        skPaint.Color = Color;
        skPaint.IsAntialias = true;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Fill;

        var skRect = new SKRect((float) drawRect.Left, (float) drawRect.Top, (float) drawRect.Right, (float) drawRect.Bottom);

        // 经过测试，似乎只有纯色画在下面才能没有锯齿，否则都会存在锯齿

        //// 以下代码经过测试，没有真的做拷贝，依然还是随着变更而变更
        //var background = new SKBitmap(new SKImageInfo((int) skRect.Width, (int) skRect.Height, _skBitmap.ColorType, _skBitmap.AlphaType));
        //using (var backgroundCanvas = new SKCanvas(background))
        //{
        //    backgroundCanvas.DrawBitmap(_skBitmap, skRect, new SKRect(0, 0, skRect.Width, skRect.Height));
        //    backgroundCanvas.Flush();
        //}

        //using var background = new SKBitmap(new SKImageInfo((int) skRect.Width, (int) skRect.Height));
        //using (var backgroundCanvas = new SKCanvas(background))
        //{
        //    skPaint.Color = new SKColor(0x12, 0x56, 0x22, 0xF1);

        //    backgroundCanvas.DrawRect(new SKRect(0, 0, skRect.Width, skRect.Height), skPaint);

        //    backgroundCanvas.DrawBitmap(SkBitmap, skRect, new SKRect(0, 0, skRect.Width, skRect.Height));

        //    backgroundCanvas.Flush();
        //}

        //skCanvas.Clear(SKColors.RosyBrown);

        //skPaint.Color = new SKColor(0x12, 0x56, 0x22, 0xF1);
        //skCanvas.DrawRect(skRect, skPaint);

        //// 似乎没有锯齿
        //skCanvas.DrawBitmap(background, new SKRect(0, 0, skRect.Width, skRect.Height), new SKRect(0, 0, skRect.Width, skRect.Height));
        //using var skImage = SKImage.FromBitmap(background);
        ////// 为何 Skia 在 DrawBitmap 之后进行 DrawPath 出现锯齿，即使配置了 IsAntialias 属性
        //skCanvas.DrawImage(skImage, new SKRect(0, 0, skRect.Width, skRect.Height), skRect);

        //// 只有纯色才能无锯齿
        // 是因为在相同的地方多次绘制采样

        //skPaint.Color = SKColors.GhostWhite;
        //skPaint.Style = SKPaintStyle.Stroke;
        //skPaint.StrokeWidth = 1f;
        skCanvas.DrawPath(skPath, skPaint);

        //skPaint.Style = SKPaintStyle.Fill;
        //skPaint.Color = SKColors.White;
        //foreach (var stylusPoint in pointList)
        //{
        //    skCanvas.DrawCircle((float) stylusPoint.Point.X, (float) stylusPoint.Point.Y, 1, skPaint);
        //}

        //skPaint.Style = SKPaintStyle.Fill;
        //skPaint.Color = SKColors.Coral;
        //foreach (var point in outlinePointList)
        //{
        //    skCanvas.DrawCircle((float) point.X, (float) point.Y, 2, skPaint);

        //}
        drawRect = new Rect(0, 0, 600, 600);

        return true;
    }

    private Point[]? _outlinePointList;

    public SKColor Color { get; set; } = SKColors.Red;

    private const int MaxStylusCount = 7;
    private readonly FixedQueue<StylusPoint> _stylusPoints = new FixedQueue<StylusPoint>(MaxStylusCount);
    private readonly StylusPoint[] _cache = new StylusPoint[MaxStylusCount + 1];
}
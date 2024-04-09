#nullable enable
using BujeeberehemnaNurgacolarje;

using Microsoft.Maui.Graphics;

using SkiaSharp;

namespace ReewheaberekaiNayweelehe;

record InkingInputInfo(int Id, StylusPoint StylusPoint, ulong Timestamp);

class SkInkCanvas
{
    public void SetCanvas(SKCanvas canvas)
    {
        _skCanvas = canvas;
    }
    private SKCanvas? _skCanvas;

    public SKBitmap? SkBitmap { set; get; }

    //public SKSurface? SkSurface { set; get; }

    public event EventHandler<Rect>? RenderBoundsChanged;

    private Dictionary<int, DrawStrokeContext> CurrentInputDictionary { get; } = new Dictionary<int, DrawStrokeContext>();

    private const int MaxStylusCount = 7;
    /// <summary>
    /// 绘制使用的上下文信息
    /// </summary>
    /// <param name="inputInfo"></param>
    class DrawStrokeContext(InkingInputInfo inputInfo)
    {
        public InkingInputInfo InputInfo { set; get; } = inputInfo;
        public int DropPointCount { set; get; }
        public readonly FixedQueue<StylusPoint> StylusPoints = new FixedQueue<StylusPoint>(MaxStylusCount);
    }
    private readonly StylusPoint[] _cache = new StylusPoint[MaxStylusCount + 1];

    public void Down(InkingInputInfo info)
    {
        CurrentInputDictionary.Add(info.Id, new DrawStrokeContext(info));
    }

    public void Move(InkingInputInfo info)
    {
        var context = UpdateInkingStylusPoint(info);

        if (DrawStroke(context, out var rect))
        {
            RenderBoundsChanged?.Invoke(this, rect);
        }
    }

    public void Up(InkingInputInfo info)
    {
        var context = UpdateInkingStylusPoint(info);
        if (CurrentInputDictionary.Remove(info.Id))
        {
            if (DrawStroke(context, out var rect))
            {
                RenderBoundsChanged?.Invoke(this, rect);
            }

            context.DropPointCount = 0;
            context.StylusPoints.Clear();
        }
        else
        {
            // 诡异的输出内容
        }
    }

    private DrawStrokeContext UpdateInkingStylusPoint(InkingInputInfo info)
    {
        if (CurrentInputDictionary.TryGetValue(info.Id, out var context))
        {
            var lastInfo = context.InputInfo;
            var stylusPoint = info.StylusPoint;
            var lastStylusPoint = lastInfo.StylusPoint;
            stylusPoint = stylusPoint with
            {
                Pressure = stylusPoint.IsPressureEnable ? stylusPoint.Pressure : lastStylusPoint.Pressure,
                Width = stylusPoint.Width ?? lastStylusPoint.Width,
                Height = stylusPoint.Height ?? lastStylusPoint.Height,
            };

            info = info with
            {
                StylusPoint = stylusPoint
            };

            context.InputInfo = info;
            return context;
        }
        else
        {
            context = new DrawStrokeContext(info);
            CurrentInputDictionary.Add(info.Id, context);
            return context;
        }
    }

    public void Move(Point point)
    {
        var x = point.X;
        var y = point.Y;
        var currentStylusPoint = new StylusPoint(x, y);

        Move(currentStylusPoint);
    }

    public void Move(StylusPoint point)
    {
        //if (DrawStroke(point, out var rect))
        //{
        //    RenderBoundsChanged?.Invoke(this, rect);
        //}
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

    public bool AutoSoftPen { set; get; } = true;

    private bool DrawStroke(DrawStrokeContext context, out Rect drawRect)
    {
        StylusPoint currentStylusPoint = context.InputInfo.StylusPoint;

        drawRect = Rect.Zero;
        if (context.StylusPoints.Count == 0)
        {
            context.StylusPoints.Enqueue(currentStylusPoint);

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

        //if (SkBitmap is null)
        //{
        //    return false;
        //}

        context.StylusPoints.CopyTo(_cache, 0);
        if (CanDropLastPoint(_cache.AsSpan(0, context.StylusPoints.Count), currentStylusPoint) && context.DropPointCount < 3)
        {
            // 丢点是为了让 SimpleInkRender 可以绘制更加平滑的折线。但是不能丢太多的点，否则将导致看起来断线
            context.DropPointCount++;
            return false;
        }

        context.DropPointCount = 0;

        var lastPoint = _cache[context.StylusPoints.Count - 1];
        if (currentStylusPoint == lastPoint)
        {
            return false;
        }

        _cache[context.StylusPoints.Count] = currentStylusPoint;
        context.StylusPoints.Enqueue(currentStylusPoint);

        if (AutoSoftPen)
        {
            for (int i = 0; i < 10; i++)
            {
                if (context.StylusPoints.Count - i - 1 < 0)
                {
                    break;
                }

                _cache[context.StylusPoints.Count - i - 1] = _cache[context.StylusPoints.Count - i - 1] with
                {
                    Pressure = Math.Max(Math.Min(0.1f * i, 0.5f), 0.01f)
                    //Pressure = 0.3f,
                };
            }
        }

        var pointList = _cache.AsSpan(0, context.StylusPoints.Count);

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
        //drawRect = new Rect(0, 0, 600, 600);

        return true;
    }

    private Point[]? _outlinePointList;

    public SKColor Color { get; set; } = SKColors.Red;


}
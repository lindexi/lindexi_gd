#nullable enable
using BujeeberehemnaNurgacolarje;

using Microsoft.Maui.Graphics;

using SkiaSharp;

namespace ReewheaberekaiNayweelehe;

record InkingInputInfo(int Id, StylusPoint StylusPoint, ulong Timestamp);

record InkInfo(int Id);

/// <summary>
/// 画板的配置
/// </summary>
/// <param name="EnableClippingEraser">是否允许使用裁剪方式的橡皮擦，而不是走静态笔迹层</param>
/// <param name="AutoSoftPen">是否开启自动软笔模式</param>
record SkInkCanvasSettings(bool EnableClippingEraser = true, bool AutoSoftPen = true);

class SkInkCanvas
{
    public SkInkCanvasSettings Settings { get; set; } = new SkInkCanvasSettings();

    public void SetCanvas(SKCanvas canvas)
    {
        _skCanvas = canvas;
    }

    private SKCanvas? _skCanvas;

    /// <summary>
    /// 原应用输出的内容
    /// </summary>
    public SKBitmap? ApplicationDrawingSkBitmap { set; get; }

    /// <summary>
    /// 原来的背景
    /// </summary>
    private SKBitmap? _originBackground;

    //public SKSurface? SkSurface { set; get; }

    public event EventHandler<Rect>? RenderBoundsChanged;

    private Dictionary<int, DrawStrokeContext> CurrentInputDictionary { get; } =
        new Dictionary<int, DrawStrokeContext>();

    /// <summary>
    /// 取多少个点做笔尖
    /// </summary>
    private const int MaxTipStylusCount = 7;

    /// <summary>
    /// 绘制使用的上下文信息
    /// </summary>
    /// <param name="inputInfo"></param>
    class DrawStrokeContext(InkingInputInfo inputInfo, SKColor strokeColor) : IDisposable
    {
        public SKColor StrokeColor { get; } = strokeColor;
        public InkingInputInfo InputInfo { set; get; } = inputInfo;
        public int DropPointCount { set; get; }

        /// <summary>
        /// 笔尖的点
        /// </summary>
        public readonly FixedQueue<StylusPoint> TipStylusPoints = new FixedQueue<StylusPoint>(MaxTipStylusCount);

        ///// <summary>
        ///// 整个笔迹的点，包括笔尖的点
        ///// </summary>
        //public List<StylusPoint> AllStylusPoints { get; } = new List<StylusPoint>();

        public SKPath? InkStrokePath { set; get; }

        public bool IsUp { set; get; }

        public void Dispose()
        {
            //InkStrokePath?.Dispose();
        }
    }

    private readonly StylusPoint[] _cache = new StylusPoint[MaxTipStylusCount + 1];

    private int MainInputId { set; get; }

    private void InputStart()
    {
        // 这是浅拷贝
        //_originBackground = SkBitmap?.Copy();

        if (ApplicationDrawingSkBitmap is null)
        {
            return;
        }

        _originBackground ??= new SKBitmap(new SKImageInfo(ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height, ApplicationDrawingSkBitmap.ColorType, ApplicationDrawingSkBitmap.AlphaType,
                    ApplicationDrawingSkBitmap.ColorSpace), SKBitmapAllocFlags.None);

        using var skCanvas = new SKCanvas(_originBackground);
        skCanvas.Clear();
        skCanvas.DrawBitmap(ApplicationDrawingSkBitmap, 0, 0);
    }

    public void Down(InkingInputInfo info)
    {
        CurrentInputDictionary.Add(info.Id, new DrawStrokeContext(info, Color));

        if (CurrentInputDictionary.Count == 1)
        {
            InputStart();
            MainInputId = info.Id;
        }
    }

    public void Move(InkingInputInfo info)
    {
        if (!CurrentInputDictionary.ContainsKey(info.Id))
        {
            // 如果丢失按下，那就不能画
            // 解决鼠标在其他窗口按下，然后移动到当前窗口
            return;
        }

        var context = UpdateInkingStylusPoint(info);

        if (IsInEraserMode)
        {
            if (info.Id == MainInputId)
            {
                MoveEraser(info);
            }
        }
        else
        {
            if (DrawStroke(context, out var rect))
            {
                RenderBoundsChanged?.Invoke(this, rect);
            }
        }
    }

    public void Up(InkingInputInfo info)
    {
        var context = UpdateInkingStylusPoint(info);

        if (IsInEraserMode)
        {
            if (info.Id == MainInputId)
            {
                UpEraser(info);
            }
        }
        else
        {
            if (DrawStroke(context, out var rect))
            {
                RenderBoundsChanged?.Invoke(this, rect);
            }
        }


        context.DropPointCount = 0;
        context.TipStylusPoints.Clear();

        context.IsUp = true;

        if (CurrentInputDictionary.All(t => t.Value.IsUp))
        {
            InputComplete();
        }
    }

    private void InputComplete()
    {
        // 不加这句话，将会在 foreach 里炸掉，不知道是不是 CLR 的坑
        var enumerator = CurrentInputDictionary.GetEnumerator();

        foreach (var drawStrokeContext in CurrentInputDictionary)
        {
            drawStrokeContext.Value.Dispose();
        }

        CurrentInputDictionary.Clear();

        InputCompleted?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? InputCompleted;

    /// <summary>
    /// 这是 WPF 的概念，那就继续用这个概念
    /// </summary>
    public void Leave()
    {
        InputComplete();

        if (_skCanvas is null || _originBackground is null)
        {
            return;
        }

        var skCanvas = _skCanvas;
        skCanvas.Clear(SKColors.Transparent);
        skCanvas.DrawBitmap(_originBackground, 0, 0);
        // 完全重绘，丢掉画出来的笔迹
        RenderBoundsChanged?.Invoke(this, new Rect(0, 0, _originBackground.Width, _originBackground.Height));
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
            context = new DrawStrokeContext(info, Color);
            CurrentInputDictionary.Add(info.Id, context);
            return context;
        }
    }

    // 静态笔迹层还没实现
    ///// <summary>
    ///// 静态笔迹层
    ///// </summary>
    //public List<InkInfo> StaticInkInfoList { get; } = new List<InkInfo>();



    /// <summary>
    /// 按照德熙的玄幻算法，决定传入的点是否能丢掉
    /// </summary>
    /// <param name="pointList"></param>
    /// <param name="currentStylusPoint"></param>
    /// <returns></returns>
    private bool CanDropLastPoint(Span<StylusPoint> pointList, StylusPoint currentStylusPoint)
    {
        if (pointList.Length < 2)
        {
            return false;
        }

        var lastPoint = pointList[^1];

        // 后续可以优化算法参考 Sia 的
        // 简单判断点的距离
        if (Math.Pow(lastPoint.Point.X - currentStylusPoint.Point.X, 2) +
            Math.Pow(lastPoint.Point.Y - currentStylusPoint.Point.Y, 2) < 100)
        {
            return true;
        }

        return false;
    }

    private bool DrawStroke(DrawStrokeContext context, out Rect drawRect)
    {
        StylusPoint currentStylusPoint = context.InputInfo.StylusPoint;

        drawRect = Rect.Zero;
        if (context.TipStylusPoints.Count == 0)
        {
            context.TipStylusPoints.Enqueue(currentStylusPoint);

            return false;
        }

        if (_skCanvas is null)
        {
            // 理论上不可能进入这里
            return false;
        }

        // 拷贝笔尖到缓存范围，方便计算。后续可以考虑不要缓存，减少拷贝，提升几乎可以忽略的性能
        context.TipStylusPoints.CopyTo(_cache, 0);

        if (CanDropLastPoint(_cache.AsSpan(0, context.TipStylusPoints.Count), currentStylusPoint) &&
            context.DropPointCount < 3)
        {
            // 丢点是为了让 SimpleInkRender 可以绘制更加平滑的折线。但是不能丢太多的点，否则将导致看起来断线
            context.DropPointCount++;
            return false;
        }

        context.DropPointCount = 0;

        var lastPoint = _cache[context.TipStylusPoints.Count - 1];
        if (currentStylusPoint == lastPoint)
        {
            return false;
        }

        _cache[context.TipStylusPoints.Count] = currentStylusPoint;
        context.TipStylusPoints.Enqueue(currentStylusPoint);

        // 是否开启自动模拟软笔效果
        if (Settings.AutoSoftPen)
        {
            for (int i = 0; i < 10; i++)
            {
                if (context.TipStylusPoints.Count - i - 1 < 0)
                {
                    break;
                }
                // 简单的算法…就是越靠近笔尖的点的压感越小
                _cache[context.TipStylusPoints.Count - i - 1] = _cache[context.TipStylusPoints.Count - i - 1] with
                {
                    Pressure = Math.Max(Math.Min(0.1f * i, 0.5f), 0.01f)
                    //Pressure = 0.3f,
                };
            }
        }

        var pointList = _cache.AsSpan(0, context.TipStylusPoints.Count);

        var outlinePointList = SimpleInkRender.GetOutlinePointList(pointList, 20);

        using var skPath = new SKPath();
        skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
        //skPath.Close();

        // 将计算出来的笔尖部分叠加回去原先的笔身，这个方式对画长线性能不好
        context.InkStrokePath ??= new SKPath();
        context.InkStrokePath.AddPath(skPath);

        var skPathBounds = skPath.Bounds;

        // 计算脏范围，用于渲染更新
        var additionSize = 10; // 用于设置比简单计算的范围更大一点的范围，解决重采样之后的模糊
        drawRect = new Rect(skPathBounds.Left - additionSize, skPathBounds.Top - additionSize,
            skPathBounds.Width + additionSize * 2, skPathBounds.Height + additionSize * 2);

        var skCanvas = _skCanvas;
        // 以下代码用于解决绘制的笔迹边缘锐利的问题。原因是笔迹执行了重采样，但是边缘如果没有被覆盖，则重采样的将会重复叠加，导致锐利
        // 根据 Skia 的官方文档，建议是走清空重新绘制。在不清屏的情况下，除非能够获取到原始的像素点。尽管这是能够计算的，但是先走清空开发速度比较快
        skCanvas.Clear(SKColors.Transparent);
        skCanvas.DrawBitmap(_originBackground, 0, 0);

        // 将所有的笔迹绘制出来，作为动态笔迹层。后续抬手的笔迹需要重新写入到静态笔迹层
        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 0.1f;
        skPaint.IsAntialias = true;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Fill;
        var enumerator = CurrentInputDictionary.GetEnumerator();

        foreach (var drawStrokeContext in CurrentInputDictionary)
        {
            skPaint.Color = drawStrokeContext.Value.StrokeColor;

            if (drawStrokeContext.Value.InkStrokePath is { } path)
            {
                skCanvas.DrawPath(path, skPaint);
            }
        }

        return true;
    }

    public SKColor Color { get; set; } = SKColors.Red;




    /// <summary>
    /// 进入橡皮擦模式
    /// </summary>
    public void EnterEraserMode()
    {
        IsInEraserMode = true;
    }

    private bool IsInEraserMode { set; get; }

    private void MoveEraser(InkingInputInfo info)
    {
        if (_skCanvas is not { } canvas || _originBackground is null)
        {
            return;
        }

        if (Settings.EnableClippingEraser)
        {
            if (EraserPath is null)
            {
                EraserPath = new SKPath();
                EraserPath.AddRect(new SKRect(0, 0, _originBackground.Width, _originBackground.Height));
            }
            else
            {
                EraserPath.Reset();
                EraserPath.AddRect(new SKRect(0, 0, _originBackground.Width, _originBackground.Height));
            }

            var point = info.StylusPoint.Point;
            var x = (float) point.X;
            var y = (float) point.Y;

            var width = 20;
            var height = 30;
            var skRect = new SKRect(x, y, x + width, y + height);

            using var skRoundRect = new SKPath();
            skRoundRect.AddRoundRect(skRect, 5, 5);
            //EraserPath.AddPath(skRoundRect, SKPathAddMode.Extend);
            EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);

            //using var skPaint = new SKPaint();
            //skPaint.Color = SKColors.White;

            canvas.Clear();
            canvas.Save();
            canvas.ClipPath(EraserPath);
            canvas.DrawBitmap(_originBackground, 0, 0);
            canvas.Restore();

            var addition = 20;
            RenderBoundsChanged?.Invoke(this, new Rect(skRect.Left - addition, skRect.Top - addition, skRect.Width + addition * 2, skRect.Height + addition * 2));
        }

       
    }

    private SKPath? EraserPath { set; get; }

    private void UpEraser(InkingInputInfo info)
    {
        EraserPath?.Dispose();
        EraserPath = null;
    }
}
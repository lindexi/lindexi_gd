#nullable enable
using System.Diagnostics;
<<<<<<< HEAD

using BujeeberehemnaNurgacolarje;
=======
using System.Numerics;
>>>>>>> b1618a865a21321eec61d1eb4fa7ac3eb9ddfcc5

using Microsoft.Maui.Graphics;

using SkiaSharp;
<<<<<<< HEAD
=======

using UnoInk.Inking.InkCore.Diagnostics;
using UnoInk.Inking.InkCore.Interactives;
using UnoInk.Inking.InkCore.Settings;

using static UnoInk.Inking.InkCore.RectExtension;
>>>>>>> b1618a865a21321eec61d1eb4fa7ac3eb9ddfcc5

namespace ReewheaberekaiNayweelehe;

<<<<<<< HEAD
partial record InkingInputInfo(int Id, StylusPoint StylusPoint, ulong Timestamp)
{
    public bool IsMouse { init; get; }
};

=======
/// <summary>
/// 笔迹信息 用于静态笔迹层
/// </summary>
/// <param name="Id"></param>
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
>>>>>>> de59e0fe8f7b5f8d759b6eb7df1d774ed1c0452c
partial record InkInfo(int Id);
=======
public partial record InkInfo(int Id, SKColor StrokeColor, SKPath? InkStrokePath);
>>>>>>> 01fd5aebad41efef3ec9afaaaefcd30a0d674cb0
=======
public partial record StrokesCollectionInfo(int Id, SKColor StrokeColor, SKPath? InkStrokePath);
>>>>>>> 258a60849bcee8adab16c45b2303bb5f8e096058
=======
public partial record StrokeCollectionInfo(int Id, SKColor StrokeColor, SKPath? InkStrokePath);
>>>>>>> 7e4dbbe7523d0540236fc7e1b7f8fb183179b7d8

/// <summary>
/// 画板的配置
/// </summary>
/// <param name="EnableClippingEraser">是否允许使用裁剪方式的橡皮擦，而不是走静态笔迹层</param>
/// <param name="AutoSoftPen">是否开启自动软笔模式</param>
record SkInkCanvasSettings(bool EnableClippingEraser = true, bool AutoSoftPen = true)
{
    /// <summary>
    /// 修改笔尖渲染部分配置 动态笔迹层
    /// </summary>
    public InkCanvasDynamicRenderTipStrokeType DynamicRenderType { init; get; } =
        InkCanvasDynamicRenderTipStrokeType.RenderAllTouchingStrokeWithoutTipStroke;

    /// <summary>
    /// 是否应该在橡皮擦丢点进行收集，进行一次性处理。现在橡皮擦速度慢在画图 DrawBitmap 里，而对于几何组装来说，似乎不耗时。此属性可能会降低性能
    /// </summary>
    /// 在触摸屏测试，使用兆芯机器，开启之后性能大幅降低
    public bool ShouldCollectDropErasePoint { init; get; } = true;
}

/// <summary>
/// 笔尖渲染模式
/// </summary>
enum InkCanvasDynamicRenderTipStrokeType
{
    /// <summary>
    /// 所有触摸按下的笔迹都每次重新绘制，不区分笔尖和笔身
    /// 此方式可以实现比较好的平滑效果
    /// </summary>
    RenderAllTouchingStrokeWithoutTipStroke,
}

class SkInkCanvas
{
    public SkInkCanvasSettings Settings { get; set; } = new SkInkCanvasSettings();

    public void SetCanvas(SKCanvas canvas)
    {
        _skCanvas = canvas;
    }

    public SKCanvas? SkCanvas => _skCanvas;
    private SKCanvas? _skCanvas;

    /// <summary>
    /// 原应用输出的内容
    /// </summary>
    public SKBitmap? ApplicationDrawingSkBitmap { set; get; }

    /// <summary>
    /// 原来的背景
    /// </summary>
    private SKBitmap? _originBackground;

    private bool _isOriginBackgroundDisable = false;

    //public SKSurface? SkSurface { set; get; }

    public event EventHandler<Rect>? RenderBoundsChanged;
    public void RaiseRenderBoundsChanged(Rect rect) => RenderBoundsChanged?.Invoke(this, rect);

    private Dictionary<int, DrawStrokeContext> CurrentInputDictionary { get; } =
        new Dictionary<int, DrawStrokeContext>();

<<<<<<< HEAD
    public IEnumerable<string> CurrentInkStrokePathEnumerable =>
        CurrentInputDictionary.Values.Select(t => t.InkStrokePath).Where(t => t != null).Select(t => t!.ToSvgPathData());
    public event EventHandler<StrokesCollectionInfo>? StrokesCollected;
=======
    public event EventHandler<StrokeCollectionInfo>? StrokesCollected;
>>>>>>> 7e4dbbe7523d0540236fc7e1b7f8fb183179b7d8

    public IEnumerable<SKPath> CurrentInkStrokePathEnumerable => CurrentInputDictionary.Values.Select(t => t.InkStrokePath)
        .Where(t => t != null)!;

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
<<<<<<< HEAD
=======
            // 不释放，否则另一个线程使用可能炸掉
            // 如 cee6070566964a8143b235e10f90dda9907e6e22 的测试
>>>>>>> 01fd5aebad41efef3ec9afaaaefcd30a0d674cb0
            //InkStrokePath?.Dispose();
        }
    }

    private readonly StylusPoint[] _cache = new StylusPoint[MaxTipStylusCount + 1];

    private int MainInputId { set; get; }

    private void InputStart()
    {
<<<<<<< HEAD
=======
        Console.WriteLine("==========InputStart============");

>>>>>>> ed5ef0e4ae3f39594e4f6a4108dc1cd46903a551
        // 这是浅拷贝
        //_originBackground = SkBitmap?.Copy();

        Console.WriteLine("==========InputStart============");

        if (ApplicationDrawingSkBitmap is null)
        {
            return;
        }

        _originBackground ??= new SKBitmap(new SKImageInfo(ApplicationDrawingSkBitmap.Width,
            ApplicationDrawingSkBitmap.Height, ApplicationDrawingSkBitmap.ColorType,
            ApplicationDrawingSkBitmap.AlphaType,
            ApplicationDrawingSkBitmap.ColorSpace), SKBitmapAllocFlags.None);
        _isOriginBackgroundDisable = false;

        using var skCanvas = new SKCanvas(_originBackground);
        skCanvas.Clear();
        skCanvas.DrawBitmap(ApplicationDrawingSkBitmap, 0, 0);
    }

    public void Down(InkingInputInfo info)
    {
        CurrentInputDictionary.Add(info.Id, new DrawStrokeContext(info, Color));

<<<<<<< HEAD
        if (CurrentInputDictionary.Count == 1)
=======
        Console.WriteLine($"Down {info.Position.X:0.00},{info.Position.Y:0.00} CurrentInputDictionaryCount={CurrentInputDictionary.Count}");
        _outputMove = false;

        // 以下逻辑由框架层处理
        //if (CurrentInputDictionary.Count == 1)
        //{
        //    MainInputId = info.Id;
        //}

        if ((IsInEraserMode || IsInEraserGestureMode) && !_isErasing)
>>>>>>> ed5ef0e4ae3f39594e4f6a4108dc1cd46903a551
        {
            InputStart();
            MainInputId = info.Id;
        }
    }
    
    private bool _outputMove;

    public void Move(InkingInputInfo info)
    {
        if (!CurrentInputDictionary.ContainsKey(info.Id))
        {
            // 如果丢失按下，那就不能画
            // 解决鼠标在其他窗口按下，然后移动到当前窗口
            return;
        }
        
        if (!_outputMove)
        {
            StaticDebugLogger.WriteLine($"IInputProcessor.Move {info.Position.X:0.00},{info.Position.Y:0.00}");
        }

        _outputMove = true;

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
<<<<<<< HEAD
        
        var strokesCollectionInfo = new StrokesCollectionInfo(info.Id, context.StrokeColor, context.InkStrokePath);
=======

        var strokesCollectionInfo = new StrokeCollectionInfo(info.Id, context.StrokeColor, context.InkStrokePath);
>>>>>>> 7e4dbbe7523d0540236fc7e1b7f8fb183179b7d8
        StrokesCollected?.Invoke(this, strokesCollectionInfo);

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

        _isOriginBackgroundDisable = true;

        InputCompleted?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? InputCompleted;

    /// <summary>
    /// 这是 WPF 的概念，那就继续用这个概念
    /// </summary>
    public void Leave()
    {
        InputComplete();

        if (_isOriginBackgroundDisable)
        {
            return;
        }

        if (_skCanvas is null || _originBackground is null)
        {
            Console.WriteLine(
                $"Leave-------- 进入非预期分支，除非是初始化 _skCanvas is null = {_skCanvas is null} _originBackground is null={_originBackground is null}");
            return;
        }

        Console.WriteLine("Leave--------");

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

<<<<<<< HEAD
        return false;
=======
        // 假定要丢掉倒数第一个点，所以上一个点是倒数第二个点
        var lastPoint = pointList[^2].Point;
        var currentPoint = currentStylusPoint.Point;

        var lastPointVector = new Vector2((float) lastPoint.X, (float) lastPoint.Y);
        var currentPointVector = new Vector2((float) currentPoint.X, (float) currentPoint.Y);

        var lineVector = currentPointVector - lastPointVector;
        var lineLength = lineVector.Length();

        // 如果移动距离比较长，则不丢点
        if (lineLength > 10)
        {
            return false;
        }

        var last2Point = pointList[^3].Point;
        var line2Vector = lastPointVector - new Vector2((float) last2Point.X, (float) last2Point.Y);
        var line2Length = line2Vector.Length();
        var vector2 = currentPointVector - lastPointVector;
        var distance2 = MathF.Abs(line2Vector.X * vector2.Y - line2Vector.Y * vector2.X) / line2Length;
        if (distance2 > 2)
        {
            return false;
        }

        return true;
>>>>>>> b1618a865a21321eec61d1eb4fa7ac3eb9ddfcc5
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

<<<<<<< HEAD
        using var skPath = new SKPath();
=======
        using var skPath = new SKPath() { FillType = SKPathFillType.Winding };
>>>>>>> b1618a865a21321eec61d1eb4fa7ac3eb9ddfcc5
        skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
        //skPath.Close();

        if (Settings.DynamicRenderType == InkCanvasDynamicRenderTipStrokeType.RenderAllTouchingStrokeWithoutTipStroke)
        {
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
<<<<<<< HEAD
            // 根据 Skia 的官方文档，建议是走清空重新绘制。在不清屏的情况下，除非能够获取到原始的像素点。尽管这是能够计算的，但是先走清空开发速度比较快
            skCanvas.Clear(SKColors.Transparent);
            skCanvas.DrawBitmap(_originBackground, 0, 0);
=======
            // 使用裁剪画布代替 Clear 方法，优化其性能
            var skRectI = SKRectI.Create((int) Math.Floor(drawRect.X), (int) Math.Floor(drawRect.Y),
                (int) Math.Ceiling(drawRect.Width), (int) Math.Ceiling(drawRect.Height));

            skRectI = LimitRect(skRectI,
                SKRectI.Create(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));

            if (_originBackground is null)
            {
                return false;
            }

            //ApplicationDrawingSkBitmap.ClearBounds(skRectI);
            var success = ApplicationDrawingSkBitmap.ReplacePixels(_originBackground, skRectI);
            if (!success)
            {
                StaticDebugLogger.WriteLine(
                    $"ReplacePixels Fail Rect={skRectI.Left},{skRectI.Top},{skRectI.Right},{skRectI.Bottom} wh={skRectI.Width},{skRectI.Height} BitmapWH={ApplicationDrawingSkBitmap.Width},{ApplicationDrawingSkBitmap.Height} D={ApplicationDrawingSkBitmap.RowBytes == (ApplicationDrawingSkBitmap.Width * sizeof(uint))}");
            }

            stepCounter.Record("ReplacePixels");

            //ApplicationDrawingSkBitmap.ClearBounds(new SKRectI(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));
            //skCanvas.Clear();
            //ApplicationDrawingSkBitmap.NotifyPixelsChanged();
            // 调用 Discard 没有任何的优化
            //skCanvas.Discard();
            SKRect skRect = skRectI;

            //stepCounter.Record("ClearBounds");

            //skCanvas.DrawBitmap(_originBackground, skRect, skRect);

            //stepCounter.Record("DrawBitmap");

            // 需要裁剪的原因是解决画完一笔之后，再画第二笔会看到第一笔不平滑
            // 效果上和第一笔所在的图片被多次绘制导致采样不平滑
            // 实际原因是在以下代码里面不断绘制整个笔迹，导致在当前画布里面就是不平滑的 但是此时渲染范围不包括整个笔迹
            // 就看不到已经不平滑的笔迹部分内容
            // 第二笔开始画的时候 更新了背景 此时的背景就包含了之前看不到的不平滑的笔迹部分内容 导致第二笔更新渲染范围将不平滑的笔迹在背景画出来 从而看到第一笔不平滑
            skCanvas.Save();
            skCanvas.ClipRect(skRect, antialias: true);
>>>>>>> b1618a865a21321eec61d1eb4fa7ac3eb9ddfcc5

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

        return false;
    }
    
    private const int DefaultAdditionSize = 4;

    public void CleanStroke(IReadOnlyList<StrokeCollectionInfo> cleanList)
    {
        SKRect drawRect = default;
        bool isFirst = true;
        foreach (var strokeCollectionInfo in cleanList)
        {
            if (strokeCollectionInfo.InkStrokePath == null)
            {
                continue;
            }
            
            if (isFirst)
            {
                drawRect = strokeCollectionInfo.InkStrokePath.Bounds;
            }
            else
            {
                drawRect.Union(strokeCollectionInfo.InkStrokePath.Bounds);
            }
            
            isFirst = false;
        }

        var skCanvas = _skCanvas;
        
        skCanvas.Clear();

        RenderBoundsChanged?.Invoke(this, Expand(drawRect, DefaultAdditionSize));

        return;
        skCanvas.DrawBitmap(_originBackground, 0, 0);

        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 0.1f;
        skPaint.IsAntialias = true;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Fill;
        
        //Console.WriteLine($"CurrentInputDictionary Count={CurrentInputDictionary.Count}");
        // 有个奇怪的炸掉情况，先忽略
        using var enumerator = CurrentInputDictionary.GetEnumerator();

        // 这里逻辑比较渣，因为可能存在 CurrentInputDictionary 被删除内容
        foreach (var drawStrokeContext in CurrentInputDictionary)
        {
            if (cleanList.Any(t=>t.Id == drawStrokeContext.Key))
            {
                continue;
            }

            skPaint.Color = drawStrokeContext.Value.StrokeColor;
            
            if (drawStrokeContext.Value.InkStrokePath is { } path)
            {
                skCanvas.DrawPath(path, skPaint);
            }
        }
    }

    public SKColor Color { get; set; } = SKColors.Red;

    // 以下是橡皮擦系列逻辑
    // 橡皮擦根据给定尺寸缩放

    /// <summary>
    /// 进入橡皮擦模式
    /// </summary>
    public void EnterEraserMode()
    {
        IsInEraserMode = true;
    }

    public void EnterPenMode()
    {
        IsInEraserMode = false;
    }

    private bool IsInEraserMode { set; get; }

    /// <summary>
    /// 移动橡皮擦的计时器，用于丢点
    /// </summary>
    private Stopwatch MoveEraserStopwatch { get; } = new Stopwatch();

    private void MoveEraser(InkingInputInfo info)
    {
        if (_skCanvas is not { } canvas || _originBackground is null)
        {
            return;
        }

<<<<<<< HEAD
        if (Settings.EnableClippingEraser)
=======
        if (!_eraserStartTime.IsRunning
            // 如果一直跟随触摸尺寸，则一直不要启动 Stopwatch 即可
            && !Settings.CanEraserAlwaysFollowsTouchSize
            && Settings.EnableStylusSizeAsEraserSize)
        {
            _eraserStartTime.Start();
        }

        double width = Settings.EraserSize.Width;
        double height = Settings.EraserSize.Height;

        // 禁止根据触摸尺寸修改橡皮擦尺寸
        var disableResizeByTouch = !Settings.CanEraserAlwaysFollowsTouchSize
                                   && _lastEraserTouchSize is not null
                                   && _eraserStartTime.Elapsed > Settings.EraserCanResizeDuringTimeSpan;

        if (disableResizeByTouch)
        {
            //StaticDebugLogger.WriteLine($"_eraserStartTime.Elapsed > Settings.EraserCanResizeDuringTimeSpan {_eraserStartTime.Elapsed}");
            // 如果不是能够一直跟随橡皮擦，且超过指定时间，则固定橡皮擦尺寸
            (width, height) = _lastEraserTouchSize!.Value;
        }
        else if (Settings.EnableStylusSizeAsEraserSize && IsInEraserGestureMode)
        {
            //StaticDebugLogger.WriteLine($"Settings.EnableStylusSizeAsEraserSizeInEraserGestureMode && IsInEraserGestureMode");
            // 由于前置保证了如果当前点没有宽度高度则使用前一个点的宽度高度
            // 所以这里判断宽度存在则设置即可，不需要处理不存在的情况
            if (info.StylusPoint.Width is not null)
            {
                width = info.StylusPoint.Width.Value;
                // 规约高度，让橡皮擦保持比例
                height = width / SkInkCanvasSettings.DefaultEraserSize.Width *
                         SkInkCanvasSettings.DefaultEraserSize.Height;
            }
            else
            {
                // 理论上进入手势橡皮擦模式，是不会有宽高是 0 的情况
            }
        }

        if (Settings.LockMinEraserSize)
        {
            // 锁定最小橡皮擦
            // 有人嫌弃小咯，那就改大点咯
            width = Math.Max(width, Settings.MinEraserSize.Width);
            height = Math.Max(height, Settings.MinEraserSize.Height);
        }

        //StaticDebugLogger.WriteLine($"MoveEraser {width},{height}");

        _lastEraserTouchSize = (width, height);

        if (Settings.EraserMode == InkCanvasEraserAlgorithmMode.EnableClippingEraserWithBinaryWithoutEraserPathCombine)
        {
            // 算法原理：
            // 走蒙层裁剪的方式
            // 首次先拍摄当前的界面作为背景图
            // 接着再通过 SKPath 镂空 Clip 的方式，填充背景图
            // 在填充和镂空之前，不执行 Clear 而是代替为二进制处理删除界面内容，减少填充范围
            // 完成这一步之后调用 Flush 刷新到界面
            // 再次拍摄当前的界面作为背景图，用于给下一次使用
            // 以上的首次和下一次，指的是橡皮擦的 MoveEraser 方法的首次和下一次调用
            // 接着再画上橡皮擦的图标
            // 如此即可实现较快速度的橡皮擦，原因是每次只需要做当前的 SKPath 镂空 Clip 的方式，填充背景图
            // 不需要计算之前的点，不会存在越擦就越慢的问题
            // 但是会带来非常多的位图拷贝逻辑，如果这套算法放在 Win 下，肯定是不行的。但是在兆芯机器上还行

            // 这个橡皮擦还没完成，存在问题
            // 1. 擦的时候，会出现部分范围被多余的擦掉，即黑边情况
            // 2. 抬手之后如何擦掉橡皮擦图标

            if (!MoveEraserStopwatch.IsRunning)
            {
                MoveEraserStopwatch.Restart();
            }
            else if (MoveEraserStopwatch.Elapsed < Settings.EraserDropPointTimeSpan)
            {
                // 如果时间距离过近，则忽略
                // 由于触摸屏大量触摸点输入，而 DrawBitmap 需要 20 毫秒，导致性能过于差
                if (Settings.ShouldCollectDropErasePoint)
                {
                    _eraserDropPointList ??= new List<StylusPoint>();
                    _eraserDropPointList.Add(info.StylusPoint);
                }

                return;
            }
            else
            {
                MoveEraserStopwatch.Restart();
            }

            if (EraserPath is null)
            {
                EraserPath = new SKPath();
            }
            else
            {
                EraserPath.Reset();
            }

            using var skPaint = new SKPaint();
            skPaint.Color = SKColors.Red;
            skPaint.Style = SKPaintStyle.Fill;

            var point = info.StylusPoint.Point;
            var x = (float) point.X;
            var y = (float) point.Y;

            x -= (float) width / 2;
            y -= (float) height / 2;

            var skRect = new SKRect(x, y, (float) (x + width), (float) (y + height));

            using var skRoundRect = new SKPath();
            skRoundRect.AddRoundRect(skRect, 5, 5);

            // 比擦掉的范围更大的范围，用于持续更新
            var expandRect = ExpandSKRect(skRect, 10);
            if (_lastEraserRenderBounds is not null)
            {
                // 理论上此时需要从原先的拷贝覆盖，否则将不能清掉上次的橡皮擦内容
                // 重新绘制 _origin 的，用于修复清理的问题
                // 为什么其他的模式不需要？原因是其他的模式的裁剪是全部的
                // 用于修复橡皮擦图标没有删除
                expandRect.Union(_lastEraserRenderBounds.Value.ToSkRect());
            }

            expandRect = LimitRectInAppBitmapRect(expandRect);
            // 先修改其为更大的尺寸，再执行 Round 可以完全确保更新在范围之内
            // 使用 SKRectI 确保像素相同
            var redrawRect = SKRectI.Round(expandRect);

            // 裁剪范围能够更小一些
            EraserPath.AddRect(redrawRect);
            EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);

            //// 几何裁剪本身无视顺序，因此先处理当前点再处理之前的点也是正确的
            //if (Settings.ShouldCollectDropErasePoint && _eraserDropPointList != null)
            //{
            //    double collectedEraserWidth = width;
            //    double collectedEraserHeight = height;

            //    // 如果有收集丢点的点，则加入计算
            //    foreach (var stylusPoint in _eraserDropPointList)
            //    {
            //        var dropPoint = stylusPoint.Point;
            //        var xDropPoint = (float) dropPoint.X;
            //        var yDropPoint = (float) dropPoint.Y;

            //        if (!disableResizeByTouch && Settings.EnableStylusSizeAsEraserSizeInEraserGestureMode && IsInEraserGestureMode)
            //        {
            //            if (stylusPoint.Width is not null)
            //            {
            //                collectedEraserWidth = stylusPoint.Width.Value;

            //                // 规约高度，让橡皮擦保持比例
            //                collectedEraserHeight = collectedEraserWidth / SkInkCanvasSettings.DefaultEraserSize.Width *
            //                                        SkInkCanvasSettings.DefaultEraserSize.Height;
            //            }
            //        }

            //        xDropPoint -= (float) collectedEraserWidth / 2;
            //        yDropPoint -= (float) collectedEraserHeight / 2;

            //        var skRectDropPoint = new SKRect(xDropPoint, yDropPoint, (float) (xDropPoint + collectedEraserWidth),
            //            (float) (yDropPoint + collectedEraserHeight));

            //        skRect.Union(skRectDropPoint);

            //        skRoundRect.Reset();
            //        skRoundRect.AddRoundRect(skRectDropPoint, 5, 5);

            //        EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);
            //    }

            //    _eraserDropPointList.Clear();
            //}

            // 更新范围
            var addition = 20;

            // 更新渲染范围为更大的范围
            var currentEraserRenderBounds = new Rect(skRect.Left - addition, skRect.Top - addition,
                skRect.Width + addition * 2,
                skRect.Height + addition * 2);
            currentEraserRenderBounds = LimitRectInAppBitmapRect(currentEraserRenderBounds);
            var rect = currentEraserRenderBounds;

            if (_lastEraserRenderBounds != null)
            {
                // 将上次的绘制范围进行重新绘制，防止出现橡皮擦图标
                skRect.Union(new SKRect((float) _lastEraserRenderBounds.Value.Left,
                    (float) _lastEraserRenderBounds.Value.Top, (float) _lastEraserRenderBounds.Value.Right,
                    (float) _lastEraserRenderBounds.Value.Bottom));
            }

            // 清理的范围应该比更新范围更小
            ApplicationDrawingSkBitmap.ClearBounds(redrawRect);

            // 可选拷贝外面一圈，用来修复黑边问题
            //ApplicationDrawingSkBitmap.ReplacePixels


            //// 裁剪范围应该和绘制范围一样大
            //skRect = ExpandSKRect(skRect, addition / 2f);

            ////// 减少裁剪范围和绘制范围，用于提升性能
            //skRoundRect.Reset();
            //skRoundRect.AddRect(skRect);
            //// 只有 skRect 范围内的才能被裁剪，而不是一开始的全画面
            //EraserPath.Op(skRoundRect, SKPathOp.Intersect, EraserPath);

            //canvas.Clear();
            canvas.Save();
            canvas.ClipPath(EraserPath, antialias: true);

            //canvas.DrawPath(EraserPath, skPaint);
            canvas.DrawBitmap(_originBackground, skRect, skRect);
            canvas.Restore();

            canvas.Flush();

            //重新更新 _originBackground 的内容，需要在画出橡皮擦之前
            using var skCanvas = new SKCanvas(_originBackground);
            skCanvas.Clear();
            skCanvas.DrawBitmap(ApplicationDrawingSkBitmap, 0, 0);

            // 画出橡皮擦
            canvas.Save();
            canvas.Translate(x, y);
            EraserView.DrawEraserView(canvas, (int) width, (int) height);
            canvas.Restore();

            if (_lastEraserRenderBounds != null)
            {
                // 将上次的绘制范围进行重新绘制，防止出现橡皮擦图标
                rect = rect.Union(_lastEraserRenderBounds.Value);
            }

            rect = LimitRectInAppBitmapRect(rect);
            _lastEraserRenderBounds = currentEraserRenderBounds;

            //// 调试下，尝试绘制整个界面
            //rect = new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height);
            RenderBoundsChanged?.Invoke(this, rect);

            MoveEraserStopwatch.Stop();
            //Console.WriteLine($"EraserPath time={MoveEraserStopwatch.ElapsedMilliseconds}ms RenderBounds={rect.X} {rect.Y} {rect.Width} {rect.Height} EraserPathPointCount={EraserPath.PointCount}");
            MoveEraserStopwatch.Restart();
        }
        else if (Settings.EraserMode == InkCanvasEraserAlgorithmMode.EnableClippingEraserWithoutEraserPathCombine)
        {
            // 算法原理：
            // 走蒙层裁剪的方式
            // 首次先拍摄当前的界面作为背景图
            // 接着再通过 SKPath 镂空 Clip 的方式，填充背景图
            // 完成这一步之后调用 Flush 刷新到界面
            // 再次拍摄当前的界面作为背景图，用于给下一次使用
            // 以上的首次和下一次，指的是橡皮擦的 MoveEraser 方法的首次和下一次调用
            // 接着再画上橡皮擦的图标
            // 如此即可实现较快速度的橡皮擦，原因是每次只需要做当前的 SKPath 镂空 Clip 的方式，填充背景图
            // 不需要计算之前的点，不会存在越擦就越慢的问题
            // 但是会带来非常多的位图拷贝逻辑，如果这套算法放在 Win 下，肯定是不行的。但是在兆芯机器上还行

            if (!MoveEraserStopwatch.IsRunning)
            {
                MoveEraserStopwatch.Restart();
            }
            else if (MoveEraserStopwatch.Elapsed < Settings.EraserDropPointTimeSpan)
            {
                // 如果时间距离过近，则忽略
                // 由于触摸屏大量触摸点输入，而 DrawBitmap 需要 20 毫秒，导致性能过于差
                if (Settings.ShouldCollectDropErasePoint)
                {
                    _eraserDropPointList ??= new List<StylusPoint>();
                    _eraserDropPointList.Add(info.StylusPoint);
                }

                return;
            }
            else
            {
                MoveEraserStopwatch.Restart();
            }

            if (EraserPath is null)
            {
                EraserPath = new SKPath();
            }
            else
            {
                EraserPath.Reset();
            }

            EraserPath.AddRect(new SKRect(0, 0, _originBackground.Width, _originBackground.Height));

            // 几何裁剪本身无视顺序，因此先处理当前点再处理之前的点也是正确的
            using var skRoundRect = new SKPath();

            var point = info.StylusPoint.Point;
            var x = (float) point.X;
            var y = (float) point.Y;

            x -= (float) width / 2;
            y -= (float) height / 2;

            var skRect = new SKRect(x, y, (float) (x + width), (float) (y + height));

            skRoundRect.AddRoundRect(skRect, 5, 5);
            EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);

            if (Settings.ShouldCollectDropErasePoint && _eraserDropPointList != null)
            {
                double collectedEraserWidth = width;
                double collectedEraserHeight = height;

                // 如果有收集丢点的点，则加入计算
                foreach (var stylusPoint in _eraserDropPointList)
                {
                    var dropPoint = stylusPoint.Point;
                    var xDropPoint = (float) dropPoint.X;
                    var yDropPoint = (float) dropPoint.Y;

                    if (!disableResizeByTouch && Settings.EnableStylusSizeAsEraserSize && IsInEraserGestureMode)
                    {
                        if (stylusPoint.Width is not null)
                        {
                            collectedEraserWidth = stylusPoint.Width.Value;

                            // 规约高度，让橡皮擦保持比例
                            collectedEraserHeight = collectedEraserWidth / SkInkCanvasSettings.DefaultEraserSize.Width *
                                                    SkInkCanvasSettings.DefaultEraserSize.Height;
                        }
                    }

                    xDropPoint -= (float) collectedEraserWidth / 2;
                    yDropPoint -= (float) collectedEraserHeight / 2;

                    var skRectDropPoint = new SKRect(xDropPoint, yDropPoint, (float) (xDropPoint + collectedEraserWidth),
                        (float) (yDropPoint + collectedEraserHeight));

                    skRect.Union(skRectDropPoint);

                    skRoundRect.Reset();
                    skRoundRect.AddRoundRect(skRectDropPoint, 5, 5);

                    EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);
                }

                _eraserDropPointList.Clear();
            }

            canvas.Clear();
            canvas.Save();
            canvas.ClipPath(EraserPath, antialias: true);
            canvas.DrawBitmap(_originBackground, 0, 0);
            canvas.Restore();

            canvas.Flush();
            // 重新更新 _originBackground 的内容，需要在画出橡皮擦之前
            //using var skCanvas = new SKCanvas(_originBackground);
            //skCanvas.Clear();
            //skCanvas.DrawBitmap(ApplicationDrawingSkBitmap, 0, 0);
            _originBackground.ReplacePixels(ApplicationDrawingSkBitmap);

            // 画出橡皮擦
            canvas.Save();
            canvas.Translate(x, y);
            EraserView.DrawEraserView(canvas, (int) width, (int) height);
            canvas.Restore();

            // 更新范围
            var addition = 20;
            var currentEraserRenderBounds = new Rect(skRect.Left - addition, skRect.Top - addition,
                skRect.Width + addition * 2,
                skRect.Height + addition * 2);
            currentEraserRenderBounds = LimitRectInAppBitmapRect(currentEraserRenderBounds);

            var rect = currentEraserRenderBounds;

            if (_lastEraserRenderBounds != null)
            {
                // 将上次的绘制范围进行重新绘制，防止出现橡皮擦图标
                rect = rect.Union(_lastEraserRenderBounds.Value);
            }

            rect = LimitRectInAppBitmapRect(rect);

            _lastEraserRenderBounds = currentEraserRenderBounds;
            RenderBoundsChanged?.Invoke(this, rect);

            MoveEraserStopwatch.Stop();
            //Console.WriteLine($"EraserPath time={MoveEraserStopwatch.ElapsedMilliseconds}ms RenderBounds={rect.X} {rect.Y} {rect.Width} {rect.Height} EraserPathPointCount={EraserPath.PointCount}");
            MoveEraserStopwatch.Restart();
        }
        else if (Settings.EraserMode == InkCanvasEraserAlgorithmMode.EnableClippingEraser)
>>>>>>> b1618a865a21321eec61d1eb4fa7ac3eb9ddfcc5
        {
            if (!MoveEraserStopwatch.IsRunning)
            {
                MoveEraserStopwatch.Restart();
            }
            else if (MoveEraserStopwatch.Elapsed < TimeSpan.FromMilliseconds(20))
            {
                // 如果时间距离过近，则忽略
                // 由于触摸屏大量触摸点输入，而 DrawBitmap 需要 20 毫秒，导致性能过于差
                if (Settings.ShouldCollectDropErasePoint)
                {
                    _eraserDropPointList ??= new List<StylusPoint>();
                    _eraserDropPointList.Add(info.StylusPoint);
                }

                return;
            }
            else
            {
                MoveEraserStopwatch.Restart();
            }

            if (EraserPath is null)
            {
                EraserPath = new SKPath();
                EraserPath.AddRect(new SKRect(0, 0, _originBackground.Width, _originBackground.Height));
            }

            double width = 30;
            double height = 45;

            using var skRoundRect = new SKPath();

<<<<<<< HEAD
=======
            var point = info.StylusPoint.Point;
            var x = (float) point.X;
            var y = (float) point.Y;

            x -= (float) width / 2;
            y -= (float) height / 2;

            var skRect = new SKRect(x, y, (float) (x + width), (float) (y + height));
            skRoundRect.AddRoundRect(skRect, 5, 5);
            EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);

            // 几何裁剪本身无视顺序，因此先处理当前点再处理之前的点也是正确的
>>>>>>> b1618a865a21321eec61d1eb4fa7ac3eb9ddfcc5
            if (Settings.ShouldCollectDropErasePoint && _eraserDropPointList != null)
            {
                // 如果有收集丢点的点，则加入计算
                foreach (var stylusPoint in _eraserDropPointList)
                {
                    var dropPoint = stylusPoint.Point;
                    var xDropPoint = (float) dropPoint.X;
                    var yDropPoint = (float) dropPoint.Y;

                    xDropPoint -= (float) width / 2;
                    yDropPoint -= (float) height / 2;

<<<<<<< HEAD
                    var skRectDropPoint = new SKRect(xDropPoint, yDropPoint, (float) (xDropPoint + width), (float) (yDropPoint + height));
=======
                    xDropPoint -= (float) collectedEraserWidth / 2;
                    yDropPoint -= (float) collectedEraserHeight / 2;

                    var skRectDropPoint = new SKRect(xDropPoint, yDropPoint, (float) (xDropPoint + collectedEraserWidth),
                        (float) (yDropPoint + collectedEraserHeight));
                    skRect.Union(skRectDropPoint);
>>>>>>> b1618a865a21321eec61d1eb4fa7ac3eb9ddfcc5

                    skRoundRect.Reset();
                    skRoundRect.AddRoundRect(skRectDropPoint, 5, 5);

                    EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);
                }

                _eraserDropPointList.Clear();
            }

            var point = info.StylusPoint.Point;
            var x = (float) point.X;
            var y = (float) point.Y;

            x -= (float) width / 2;
            y -= (float) height / 2;

            var skRect = new SKRect(x, y, (float) (x + width), (float) (y + height));

            skRoundRect.AddRoundRect(skRect, 5, 5);
            EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);

            canvas.Clear();
            canvas.Save();
            canvas.ClipPath(EraserPath, antialias: true);
            canvas.DrawBitmap(_originBackground, 0, 0);
            canvas.Restore();

            // 画出橡皮擦
            canvas.Save();
            canvas.Translate(x, y);
<<<<<<< HEAD
            EraserView.DrawEraserView(canvas, 30, 45);
=======
            EraserView.DrawEraserView(canvas, (int) width, (int) height);
>>>>>>> b1618a865a21321eec61d1eb4fa7ac3eb9ddfcc5
            canvas.Restore();

            // 更新范围
            var addition = 20;
            var currentEraserRenderBounds = new Rect(skRect.Left - addition, skRect.Top - addition, skRect.Width + addition * 2,
                skRect.Height + addition * 2);

            var rect = currentEraserRenderBounds;

            if (_lastEraserRenderBounds != null)
            {
                // 如果将上次的绘制范围进行重新绘制，防止出现橡皮擦图标
                rect = rect.Union(_lastEraserRenderBounds.Value);
            }

            _lastEraserRenderBounds = currentEraserRenderBounds;
            RenderBoundsChanged?.Invoke(this, rect);

            MoveEraserStopwatch.Stop();
            Console.WriteLine($"EraserPath time={MoveEraserStopwatch.ElapsedMilliseconds}ms RenderBounds={rect.X} {rect.Y} {rect.Width} {rect.Height} EraserPathPointCount={EraserPath.PointCount}");
            MoveEraserStopwatch.Restart();
        }
    }

    private SKPath? EraserPath { set; get; }

    private EraserView EraserView { get; } = new EraserView();

    /// <summary>
    /// 在橡皮擦丢点进行收集，进行一次性处理
    /// </summary>
    private List<StylusPoint>? _eraserDropPointList;

    /// <summary>
    /// 上一次的橡皮擦渲染范围
    /// </summary>
    private Rect? _lastEraserRenderBounds;

    private void UpEraser(InkingInputInfo info)
    {
        if (_skCanvas is not { } canvas || _originBackground is null || EraserPath is null)
        {
            return;
        }

        canvas.Clear();

        canvas.Save();
        canvas.ClipPath(EraserPath, antialias: true);
        canvas.DrawBitmap(_originBackground, 0, 0);
        canvas.Restore();

        EraserPath?.Dispose();
        EraserPath = null;

        _lastEraserRenderBounds = null;

        // 完全重绘，修复可能存在的丢失裁剪
        RenderBoundsChanged?.Invoke(this, new Rect(0, 0, _originBackground.Width, _originBackground.Height));
    }
}

class EraserView
{
    public SKBitmap GetEraserView(int width, int height)
    {
        var skBitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));

        using var skCanvas = new SKCanvas(skBitmap);
        DrawEraserView(skCanvas, width, height);

        return skBitmap;
    }

    public void DrawEraserView(SKCanvas skCanvas, int width, int height)
    {
        var pathWidth = 30;
        var pathHeight = 45;

        using var path1 = SKPath.ParseSvgPathData(
            "M0,5.0093855C0,2.24277828,2.2303666,0,5.00443555,0L24.9955644,0C27.7594379,0,30,2.23861485,30,4.99982044L30,17.9121669C30,20.6734914,30,25.1514578,30,27.9102984L30,40.0016889C30,42.7621799,27.7696334,45,24.9955644,45L5.00443555,45C2.24056212,45,0,42.768443,0,39.9906145L0,5.0093855z");
        using var skPaint = new SKPaint();
        skPaint.IsAntialias = true;
        skPaint.Style = SKPaintStyle.Fill;
        skPaint.Color = new SKColor(0, 0, 0, 0x33);
        skCanvas.DrawPath(path1, skPaint);

        skPaint.Color = new SKColor(0xF2, 0xEE, 0xEB, 0xFF);
        skCanvas.DrawRoundRect(1, 1, 28, 43, 4, 4, skPaint);

        using var path2 = SKPath.ParseSvgPathData(
            "M20,29.1666667L20,16.1666667C20,15.3382395 19.3284271,14.6666667 18.5,14.6666667 17.6715729,14.6666667 17,15.3382395 17,16.1666667L17,29.1666667C17,29.9950938 17.6715729,30.6666667 18.5,30.6666667 19.3284271,30.6666667 20,29.9950938 20,29.1666667z M13,29.1666667L13,16.1666667C13,15.3382395 12.3284271,14.6666667 11.5,14.6666667 10.6715729,14.6666667 10,15.3382395 10,16.1666667L10,29.1666667C10,29.9950938 10.6715729,30.6666667 11.5,30.6666667 12.3284271,30.6666667 13,29.9950938 13,29.1666667z");
        skPaint.Color = new SKColor(0x00, 0x00, 0x00, 0x26);
        skCanvas.DrawPath(path2, skPaint);
    }
}
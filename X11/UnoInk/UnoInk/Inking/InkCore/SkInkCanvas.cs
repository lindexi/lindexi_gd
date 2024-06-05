using System.Diagnostics;
using System.Numerics;

using Microsoft.Maui.Graphics;

using SkiaSharp;

using UnoInk.Inking.InkCore.Diagnostics;
using UnoInk.Inking.InkCore.Interactives;
using UnoInk.Inking.InkCore.Settings;

using static UnoInk.Inking.InkCore.RectExtension;

namespace UnoInk.Inking.InkCore;

/// <summary>
/// 笔迹信息 用于静态笔迹层
/// </summary>
/// <param name="Id"></param>
public partial record StrokeCollectionInfo(int Id, SKColor StrokeColor, SKPath? InkStrokePath);

/// <summary>
/// 使用 Skia 的 Ink 笔迹画板
/// </summary>
class SkInkCanvas : IInputProcessor, IModeInputDispatcherSensitive
{
    /// <summary>
    /// 创建画板
    /// </summary>
    /// <param name="canvas">用于绘制输出的 SKCanvas 对象</param>
    /// <param name="applicationDrawingSkBitmap">被 <paramref name="canvas"/> 绘制输出的位图</param>
    public SkInkCanvas(SKCanvas canvas, SKBitmap applicationDrawingSkBitmap)
    {
        // 这里有一个限制是走 CPU 时，能够取得更快的性能，原因是有大量利用 ApplicationDrawingSkBitmap 的存在
        // 进行二进制修改渲染图
        // 如果后续使用平面绘制方法则需要重新设计
        _skCanvas = canvas;
        ApplicationDrawingSkBitmap = applicationDrawingSkBitmap;
    }

    public bool Enable => true;

    public SkInkCanvasSettings Settings { get; set; } = new SkInkCanvasSettings();

    private readonly SKCanvas _skCanvas;

    /// <summary>
    /// 原应用输出的内容
    /// </summary>
    private SKBitmap ApplicationDrawingSkBitmap { get; }

    /// <summary>
    /// 开始书写时对当前原应用输出的内容 <see cref="ApplicationDrawingSkBitmap"/> 制作的快照，用于解决笔迹的平滑处理，和笔迹算法相关
    /// </summary>
    private SKBitmap? _originBackground;

    /// <summary>
    /// 是否原来的背景，即充当静态层的界面是无效的
    /// </summary>
    private bool _isOriginBackgroundDisable = false;

    //public SKSurface? SkSurface { set; get; }

    public event EventHandler<Rect>? RenderBoundsChanged;

    private Dictionary<int, DrawStrokeContext> CurrentInputDictionary { get; } =
        new Dictionary<int, DrawStrokeContext>();

    public event EventHandler<StrokeCollectionInfo>? StrokesCollected;

    //public IEnumerable<string> CurrentInkStrokePathEnumerable =>
    //    CurrentInputDictionary.Values.Select(t => t.InkStrokePath).Where(t => t != null).Select(t => t!.ToSvgPathData());

    /// <summary>
    /// 取多少个点做笔尖
    /// </summary>
    /// 经验值，原本只是想取 5 + 1 个点，但是发现这样笔尖太短了，于是再加一个点
    private const int MaxTipStylusCount = 7;

    /// <summary>
    /// 绘制使用的上下文信息
    /// </summary>
    /// <param name="inputInfo"></param>
    class DrawStrokeContext(ModeInputArgs inputInfo, SKColor strokeColor, double inkThickness) : IDisposable
    {
        public double InkThickness { get; } = inkThickness;

        public SKColor StrokeColor { get; } = strokeColor;
        public ModeInputArgs InputInfo { set; get; } = inputInfo;
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

        public bool IsLeave { set; get; }

        public void Dispose()
        {
            // 不释放，否则另一个线程使用可能炸掉
            // 如 cee6070566964a8143b235e10f90dda9907e6e22 的测试
            //InkStrokePath?.Dispose();
        }
    }

    #region IInputProcessor

    void IInputProcessor.InputStart()
    {
        Console.WriteLine("==========InputStart============");

        // 这是浅拷贝
        //_originBackground = SkBitmap?.Copy();

        // 需要使用 SKCanvas 才能实现拷贝
        _originBackground ??= new SKBitmap(new SKImageInfo(ApplicationDrawingSkBitmap.Width,
            ApplicationDrawingSkBitmap.Height, ApplicationDrawingSkBitmap.ColorType,
            ApplicationDrawingSkBitmap.AlphaType,
            ApplicationDrawingSkBitmap.ColorSpace), SKBitmapAllocFlags.None);
        _isOriginBackgroundDisable = false;

        using var skCanvas = new SKCanvas(_originBackground);
        skCanvas.Clear();
        skCanvas.DrawBitmap(ApplicationDrawingSkBitmap, 0, 0);
    }

    void IInputProcessor.Down(ModeInputArgs info)
    {
        CurrentInputDictionary.Add(info.Id, new DrawStrokeContext(info, Settings.Color, Settings.InkThickness));

        Console.WriteLine($"Down {info.Position.X:0.00},{info.Position.Y:0.00} CurrentInputDictionaryCount={CurrentInputDictionary.Count}");
        _outputMove = false;

        // 以下逻辑由框架层处理
        //if (CurrentInputDictionary.Count == 1)
        //{
        //    MainInputId = info.Id;
        //}

        if ((IsInEraserMode || IsInEraserGestureMode) && !_isErasing)
        {
            // 首次就进入橡皮擦
            DownEraser(in info);
        }
    }
    
    private bool _outputMove;

    void IInputProcessor.Move(ModeInputArgs info)
    {
        if (!CurrentInputDictionary.ContainsKey(info.Id))
        {
            // 如果丢失按下，那就不能画
            // 解决鼠标在其他窗口按下，然后移动到当前窗口
            StaticDebugLogger.WriteLine($"Lost Input Id={info.Id}");
            return;
        }
        
        if (!_outputMove)
        {
            StaticDebugLogger.WriteLine($"IInputProcessor.Move {info.Position.X:0.00},{info.Position.Y:0.00}");
        }

        _outputMove = true;

        var context = UpdateInkingStylusPoint(info);
        // 重新赋值 info 值，因此旧的这个值没有处理宽度高度是空的情况，使用上一个点的宽度高度而让橡皮擦闪烁
        info = context.InputInfo;

        if (IsInEraserMode || IsInEraserGestureMode)
        {
            if (!_isErasing)
            {
                // 如果是手指按下，然后再判断是橡皮擦的，则进入此分支
                // 或者上个橡皮擦抬起，下一次执行则也进入此分支
                StaticDebugLogger.WriteLine($"[{nameof(SkInkCanvas)}] Move DownEraser");
                DownEraser(context.InputInfo);
            }

            if (info.Id == _eraserDeviceId)
            {
                MoveEraser(info);
            }

#if DEBUG
            string modeName = "NONE";
            if (IsInEraserMode && IsInEraserGestureMode)
            {
                modeName = "[IsInEraserMode&&IsInEraserGestureMode]";
            }
            else if (IsInEraserMode)
            {
                modeName = nameof(IsInEraserMode);
            }
            else if (IsInEraserGestureMode)
            {
                modeName = nameof(IsInEraserGestureMode);
            }

            if (info.Id == _eraserDeviceId)
            {
                StaticDebugLogger.WriteLine($"[{modeName}] Id==_eraserDeviceId Id={info.Id} _eraserDeviceId={_eraserDeviceId}");
            }
            else
            {
                StaticDebugLogger.WriteLine(
                    $"[{modeName}] Id!=_eraserDeviceId Id={info.Id} _eraserDeviceId={_eraserDeviceId} _eraserDeviceIdUp?={!ModeInputDispatcher.ContainsDeviceId(_eraserDeviceId)} DeviceCount={ModeInputDispatcher.CurrentDeviceCount}");
            }
#endif
        }
        else
        {
            var stylusPoint = context.InputInfo.StylusPoint;
            if (Settings.EnableEraserGesture // 启用手势橡皮擦的前提下，通过尺寸进行判断
                && stylusPoint.Width != null
                && stylusPoint.Height != null
                && stylusPoint.Width >= Settings.MinEraserGesturePixelSize.Width
                && stylusPoint.Height >= Settings.MinEraserGesturePixelSize.Height
                // 如果输入较长，则不能进入橡皮擦模式
                && ModeInputDispatcher.InputDuring > Settings.DisableEnterEraserGestureAfterInputDuring)
            {
                EnterEraserGestureMode(context.InputInfo);
                return;
            }

            if (DrawStroke(context, out var rect))
            {
                RenderBoundsChanged?.Invoke(this, rect);
            }
        }
    }

    void IInputProcessor.Hover(ModeInputArgs args)
    {
        // 没有什么作用
    }

    void IInputProcessor.Up(ModeInputArgs info)
    {
        var context = UpdateInkingStylusPoint(info);
        info = context.InputInfo;

        if (IsInEraserMode || IsInEraserGestureMode)
        {
            if (info.Id == _eraserDeviceId)
            {
                StaticDebugLogger.WriteLine($"[{nameof(SkInkCanvas)}] UpEraser _eraserDeviceId={_eraserDeviceId}");
                UpEraser(in info);
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

        var strokesCollectionInfo = new StrokeCollectionInfo(info.Id, context.StrokeColor, context.InkStrokePath);
        StrokesCollected?.Invoke(this, strokesCollectionInfo);

        if (CurrentInputDictionary.All(t => t.Value.IsUp))
        {
            //完成等待清理
        }
    }

    void IInputProcessor.InputComplete()
    {
        Clean();

        StaticDebugLogger.WriteLine($"InputComplete\r\n==========\r\n");

        InputCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void Clean()
    {
        // 不加这句话，将会在 foreach 里炸掉，不知道是不是 CLR 的坑
        using var enumerator = CurrentInputDictionary.GetEnumerator();

        foreach (var drawStrokeContext in CurrentInputDictionary)
        {
            if (!drawStrokeContext.Value.IsUp)
            {
                // 如果还没有 Up 的状态，那就是 Leave 状态了
                drawStrokeContext.Value.IsLeave = true;
            }

            drawStrokeContext.Value.Dispose();
        }

        CurrentInputDictionary.Clear();

        if (IsInEraserMode || IsInEraserGestureMode)
        {
            // 当前是橡皮擦模式，需要清理橡皮擦，修复 1382 对窗口在顶的工具条进行手势橡皮，橡皮擦不消失
            CleanEraser();
        }

        _isOriginBackgroundDisable = true;
        IsInEraserGestureMode = false;

        _isErasing = false;
    }

    public event EventHandler? InputCompleted;

    /// <summary>
    /// 这是 WPF 的概念，那就继续用这个概念
    /// </summary>
    void IInputProcessor.Leave()
    {
        StaticDebugLogger.WriteLine($"{DateTime.Now:hh-MM-ss} IInputProcessor.Leave");

        Clean();

        if (_isOriginBackgroundDisable)
        {
            // 几乎必定进入此分支，除非 Clean 方法后续改错了
            //Console.WriteLine("_isOriginBackgroundDisable=true");
            return;
        }

        if (_originBackground is null)
        {
            StaticDebugLogger.WriteLine(
                $"Leave-------- 进入非预期分支，除非是初始化 _originBackground is null={_originBackground is null}");
            return;
        }

        StaticDebugLogger.WriteLine("Leave--------");

        var skCanvas = _skCanvas;
        skCanvas.Clear(SKColors.Transparent);
        skCanvas.DrawBitmap(_originBackground, 0, 0);
        // 完全重绘，丢掉画出来的笔迹
        RenderBoundsChanged?.Invoke(this, new Rect(0, 0, _originBackground.Width, _originBackground.Height));
    }

    #endregion

    private DrawStrokeContext UpdateInkingStylusPoint(ModeInputArgs info)
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
            context = new DrawStrokeContext(info, Settings.Color, Settings.InkThickness);
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
    /// <param name="dropPointCount"></param>
    /// <returns></returns>
    private bool CanDropLastPoint(IReadOnlyList<StylusPoint> pointList, StylusPoint currentStylusPoint,
        int dropPointCount)
    {
        if (pointList.Count < 3)
        {
            return false;
        }

        // 已经丢了10个点了，就不继续丢点了
        if (dropPointCount >= 10)
        {
            return false;
        }

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

        var lastPoint = context.TipStylusPoints[^1];
        if (lastPoint.Point == currentStylusPoint.Point)
        {
            // 如果两点相同，则取最大压感
            context.TipStylusPoints[^1] = lastPoint with
            {
                Pressure = Math.Max(lastPoint.Pressure, currentStylusPoint.Pressure)
            };

            if (context.TipStylusPoints.Count == 1)
            {
                // 只有 1 个点，不够计算
                return false;
            }
        }
        else
        {
            if (CanDropLastPoint(context.TipStylusPoints, currentStylusPoint, context.DropPointCount))
            {
                currentStylusPoint = currentStylusPoint with { Pressure = context.TipStylusPoints[^1].Pressure };
                context.TipStylusPoints[^1] = currentStylusPoint;
                // 丢点是为了让 SimpleInkRender 可以绘制更加平滑的折线。但是不能丢太多的点，否则将导致看起来断线
                context.DropPointCount++;
                //Console.WriteLine($"DropPointCount {context.DropPointCount}");
            }
            else
            {
                context.DropPointCount = 0;
                context.TipStylusPoints.Enqueue(currentStylusPoint);
            }
        }

        // 是否开启自动模拟软笔效果
        if (Settings.AutoSoftPen)
        {
            const int softPenCount = 5;
            for (var i = 0; i < softPenCount; i++)
            {
                if (context.TipStylusPoints.Count - i - 1 < 0)
                {
                    break;
                }

                // 简单的算法…就是越靠近笔尖的点的压感越小
                context.TipStylusPoints[context.TipStylusPoints.Count - i - 1] =
                    context.TipStylusPoints[context.TipStylusPoints.Count - i - 1] with
                    {
                        Pressure = Math.Max(Math.Min(1.0f / softPenCount * (i + 1), 0.5f), 0.01f)
                        //Pressure = 0.3f,
                    };
            }
        }

        var pointList = context.TipStylusPoints;

        var outlinePointList = SimpleInkRender.GetOutlinePointList(pointList, context.InkThickness);

        using var skPath = new SKPath() { FillType = SKPathFillType.Winding };
        skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
        //skPath.Close();

        // 用于设置比简单计算的范围更大一点的范围，解决重采样之后的模糊
        // 设置为笔迹粗细的一半，因为后续将会上下左右都加上这个值
        var additionSize = Math.Min(context.InkThickness / 2, 4);

        var skCanvas = _skCanvas;
        if (Settings.DynamicRenderType == InkCanvasDynamicRenderTipStrokeType.RenderAllTouchingStrokeWithoutTipStroke)
        {
            // 将计算出来的笔尖部分叠加回去原先的笔身，这个方式对画长线性能不好
            context.InkStrokePath ??= new SKPath { FillType = SKPathFillType.Winding };
            context.InkStrokePath.AddPath(skPath.Simplify() ?? skPath);

            var skPathBounds = skPath.Bounds;

            // 计算脏范围，用于渲染更新
            drawRect = new Rect(skPathBounds.Left - additionSize, skPathBounds.Top - additionSize,
                skPathBounds.Width + additionSize * 2, skPathBounds.Height + additionSize * 2);
            drawRect = LimitRect(drawRect,
                new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));

            // 以下代码用于解决绘制的笔迹边缘锐利的问题。原因是笔迹执行了重采样，但是边缘如果没有被覆盖，则重采样的将会重复叠加，导致锐利
            // 根据 Skia 的官方文档，建议是走清空重新绘制。在不清屏的情况下，除非能够获取到原始的像素点。尽管这是能够计算的，但是先走清空开发速度比较快
            skCanvas.Clear();
            skCanvas.DrawBitmap(_originBackground, 0, 0);
            //Console.WriteLine($"DrawBitmap {stopwatch.ElapsedMilliseconds}");

            // 将所有的笔迹绘制出来，作为动态笔迹层。后续抬手的笔迹需要重新写入到静态笔迹层
            using var skPaint = new SKPaint();
            skPaint.StrokeWidth = 0f;
            skPaint.IsAntialias = true;
            skPaint.IsStroke = false;
            skPaint.FilterQuality = SKFilterQuality.High;
            skPaint.Style = SKPaintStyle.Fill;

            // 有个奇怪的炸掉情况，先忽略
            using var enumerator = CurrentInputDictionary.GetEnumerator();

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
        else if (Settings.DynamicRenderType == InkCanvasDynamicRenderTipStrokeType.RenderAllTouchingStrokeWithClip)
        {
            var stepCounter = new StepCounter();
            //stepCounter.Start();

            context.InkStrokePath ??= new SKPath { FillType = SKPathFillType.Winding };
            context.InkStrokePath.AddPath(skPath);

            var skPathBounds = skPath.Bounds;

            if (skPath.IsEmpty)
            {
                StaticDebugLogger.WriteLine($"skPathBounds.IsEmpty={skPath.IsEmpty}");

                StaticDebugLogger.WriteLine(skPath.ToSvgPathData());

                StaticDebugLogger.WriteLine("pointList=");
                foreach (var stylusPoint in pointList)
                {
                    StaticDebugLogger.WriteLine($"{stylusPoint.Point.X},{stylusPoint.Point.Y}");
                }

                StaticDebugLogger.WriteLine("outlinePointList=");
                foreach (var point in outlinePointList)
                {
                    StaticDebugLogger.WriteLine($"{point.X},{point.Y}");
                }

                return false;
            }

            // 计算脏范围，用于渲染更新
            drawRect = new Rect(skPathBounds.Left - additionSize, skPathBounds.Top - additionSize,
                skPathBounds.Width + additionSize * 2, skPathBounds.Height + additionSize * 2);

            System.Diagnostics.Debug.Assert(ApplicationDrawingSkBitmap != null);
            // 限制矩形范围，防止超过画布
            drawRect = LimitRect(drawRect,
                new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));

            if (drawRect.IsEmpty)
            {
                // 渲染范围是空的，则不执行任何处理
                return false;
            }

            // 以下代码用于解决绘制的笔迹边缘锐利的问题。原因是笔迹执行了重采样，但是边缘如果没有被覆盖，则重采样的将会重复叠加，导致锐利
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

            // 将所有的笔迹绘制出来，作为动态笔迹层。后续抬手的笔迹需要重新写入到静态笔迹层
            using var skPaint = new SKPaint();
            skPaint.StrokeWidth = 0.1f;
            skPaint.IsAntialias = true;
            skPaint.FilterQuality = SKFilterQuality.High;
            skPaint.Style = SKPaintStyle.Fill;

            //Console.WriteLine($"CurrentInputDictionary Count={CurrentInputDictionary.Count}");
            // 有个奇怪的炸掉情况，先忽略
            using var enumerator = CurrentInputDictionary.GetEnumerator();

            foreach (var drawStrokeContext in CurrentInputDictionary)
            {
                skPaint.Color = drawStrokeContext.Value.StrokeColor;

                if (drawStrokeContext.Value.InkStrokePath is { } path)
                {
                    skCanvas.DrawPath(path, skPaint);
                }
            }

            stepCounter.Record("DrawPath");

            skCanvas.Restore();

            stepCounter.OutputToConsole();

            return true;
        }
        else if (Settings.DynamicRenderType == InkCanvasDynamicRenderTipStrokeType.RenderTipStrokeOnly)
        {
            // 不断向前叠
            using var skPaint = new SKPaint();
            skPaint.StrokeWidth = 0.1f;
            skPaint.IsAntialias = true;
            skPaint.FilterQuality = SKFilterQuality.High;
            skPaint.Style = SKPaintStyle.Fill;
            skPaint.Color = context.StrokeColor;

            skCanvas.DrawPath(skPath, skPaint);

            // 计算脏范围，用于渲染更新
            drawRect = Expand(skPath.Bounds, 10);

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

    // 以下是橡皮擦系列逻辑

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

    public bool IsInEraserGestureMode { private set; get; }

    /// <summary>
    /// 进入橡皮擦模式
    /// </summary>
    public void EnterEraserGestureMode(in ModeInputArgs args)
    {
        IsInEraserGestureMode = true;

        // 解决手势橡皮擦无法启动问题
        // 步骤：
        // 1. 左手手指1按下
        // 2. 右手手掌2触摸
        // 3. 移动手指1然后抬起
        // 原现象：
        // 手指1移动过程出现橡皮擦，手掌2部分移动没效果
        // 抬起手指1之后，所有触摸都没有反应
        // 原因，橡皮擦采用首个触摸手指进行处理，而不是真正触发面积的手指处理，导致处理错误
        // 实际效果：有时候手势橡皮擦就是什么都没发生
        // 当前逻辑： 橡皮擦将跟随手掌2移动，等后续如果手掌2先于手指1抬起，之后手指1移动将会作为橡皮擦
        if (IsInEraserMode && _isErasing)
        {
            // 如果是橡皮擦模式，且在擦中，则啥都不需要做
            // 修复 gitlab/45
        }
        else
        {
            System.Diagnostics.Debug.Assert(!_isErasing);
            DownEraser(in args);
        }
    }

    /// <summary>
    /// 移动橡皮擦的计时器，用于丢点
    /// </summary>
    private Stopwatch MoveEraserStopwatch { get; } = new Stopwatch();

    private (double Width, double Height)? _lastEraserTouchSize;

    /// <summary>
    /// 由于橡皮擦只能支持单个手指，多个手指性能顶不住，因此需要此判断具体输入是哪个手指
    /// </summary>
    private int _eraserDeviceId;

    /// <summary>
    /// 是否正在擦中
    /// </summary>
    private bool _isErasing;

    private void DownEraser(in ModeInputArgs info)
    {
        if (_isErasing)
        {
            throw new InvalidOperationException($"重复进入橡皮擦");
        }

        _eraserDeviceId = info.Id;
        _isErasing = true;
    }

    private void MoveEraser(ModeInputArgs info)
    {
        if (_skCanvas is not { } canvas || _originBackground is null)
        {
            return;
        }

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
        {
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
                EraserPath.AddRect(new SKRect(0, 0, _originBackground.Width, _originBackground.Height));
            }

            using var skRoundRect = new SKPath();

            var point = info.StylusPoint.Point;
            var x = (float) point.X;
            var y = (float) point.Y;

            x -= (float) width / 2;
            y -= (float) height / 2;

            var skRect = new SKRect(x, y, (float) (x + width), (float) (y + height));
            skRoundRect.AddRoundRect(skRect, 5, 5);
            EraserPath.Op(skRoundRect, SKPathOp.Difference, EraserPath);

            // 几何裁剪本身无视顺序，因此先处理当前点再处理之前的点也是正确的
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
                        // 由于前置保证了如果当前点没有宽度高度则使用前一个点的宽度高度
                        // 所以这里判断宽度存在则设置即可，不需要处理不存在的情况
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
                // 如果将上次的绘制范围进行重新绘制，防止出现橡皮擦图标
                rect = rect.Union(_lastEraserRenderBounds.Value);
            }

            rect = LimitRectInAppBitmapRect(rect);

            _lastEraserRenderBounds = currentEraserRenderBounds;
            RenderBoundsChanged?.Invoke(this, rect);

            MoveEraserStopwatch.Stop();
            StaticDebugLogger.WriteLine(
                $"EraserPath time={MoveEraserStopwatch.ElapsedMilliseconds}ms RenderBounds={rect.X} {rect.Y} {rect.Width} {rect.Height} EraserPathPointCount={EraserPath.PointCount}");
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

    /// <summary>
    /// 橡皮擦开始擦的时间
    /// </summary>
    private readonly Stopwatch _eraserStartTime = new Stopwatch();

    private void UpEraser(in ModeInputArgs info)
    {
        if (info.Id != _eraserDeviceId)
        {
            throw new InvalidOperationException(
                $"抬起时的 Id 不是当前橡皮擦的 Id 值 info.Id={info.Id} _eraserDeviceId={_eraserDeviceId}");
        }

        CleanEraser();
        _isErasing = false;
    }

    /// <summary>
    /// 清理橡皮擦
    /// </summary>
    private void CleanEraser()
    {
        if (_skCanvas is not { } canvas || _originBackground is null)
        {
            return;
        }

        if (Settings.EraserMode == InkCanvasEraserAlgorithmMode.EnableClippingEraserWithBinaryWithoutEraserPathCombine)
        {
            // 这个橡皮擦需要特殊的方式清空
            // 因为 EraserPath 是一个很小的值
            return;
        }

        //StaticDebugLogger.WriteLine("UpEraser");

        var lastEraserRenderBounds = _lastEraserRenderBounds;
        _lastEraserRenderBounds = null;
        _eraserDropPointList?.Clear();

        _eraserStartTime.Stop();
        // 如果不重设，则下次开启会继续计时
        _eraserStartTime.Reset();

        if (EraserPath is null)
        {
            // 没有执行实际的橡皮擦，不需要清理画布
            return;
        }

        canvas.Clear();

        canvas.Save();
        canvas.ClipPath(EraserPath, antialias: true);
        canvas.DrawBitmap(_originBackground, 0, 0);
        canvas.Restore();

        EraserPath?.Dispose();
        EraserPath = null;

        // 由于完全重绘将会奇怪降低笔迹的速度，于是换成只处理最后的橡皮擦渲染范围
        if (lastEraserRenderBounds != null)
        {
            RenderBoundsChanged?.Invoke(this, lastEraserRenderBounds.Value);
        }
        //// 完全重绘，修复可能存在的丢失裁剪
        //RenderBoundsChanged?.Invoke(this, new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));
    }

    private bool _isDebug = false;

    public void Debug()
    {
        _isDebug = !_isDebug;

        RenderBoundsChanged?.Invoke(this,
            new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));

        // 经过测试更换画布是没有用的
        //_skCanvas = new SKCanvas(ApplicationDrawingSkBitmap);

        // 这句也没有用，由于重新更换画布都没有用，因此可以大概理解为 ApplicationDrawingSkBitmap 的问题
        _skCanvas.Flush();

        // 看起来也是没有用的，速度依然很慢
        _originBackground?.Dispose();
        _originBackground = null;
    }

    private Rect LimitRectInAppBitmapRect(Rect inputRect)
    {
        return LimitRect(inputRect,
            new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));
    }

    private SKRect LimitRectInAppBitmapRect(SKRect inputRect)
    {
        return LimitRect(inputRect,
            new SKRect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));
    }

    public ModeInputDispatcher ModeInputDispatcher { set; get; }
    // 框架层赋值
        = null!;
}

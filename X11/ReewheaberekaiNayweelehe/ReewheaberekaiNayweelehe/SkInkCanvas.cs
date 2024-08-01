#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

using BujeeberehemnaNurgacolarje;

using dotnetCampus.Mathematics.SpatialGeometry;

using SkiaInkCore;
using SkiaInkCore.Diagnostics;
using SkiaInkCore.Interactives;
using SkiaInkCore.Primitive;
using SkiaInkCore.Settings;
using SkiaInkCore.Utils;

using SkiaSharp;

namespace ReewheaberekaiNayweelehe;

partial class SkInkCanvas : IInkingInputProcessor, IInkingModeInputDispatcherSensitive
{
    public SkInkCanvas(SKCanvas skCanvas, SKBitmap applicationDrawingSkBitmap)
    {
        _skCanvas = skCanvas;
        ApplicationDrawingSkBitmap = applicationDrawingSkBitmap;
    }

    public SkInkCanvasSettings Settings { get; set; } = new SkInkCanvasSettings();

    public event EventHandler<Rect>? RenderBoundsChanged;

    private readonly SKCanvas _skCanvas;

    /// <summary>
    /// 原应用输出的内容
    /// </summary>
    public SKBitmap ApplicationDrawingSkBitmap { set; get; }

    /// <summary>
    /// 开始书写时对当前原应用输出的内容 <see cref="ApplicationDrawingSkBitmap"/> 制作的快照，用于解决笔迹的平滑处理，和笔迹算法相关
    /// </summary>
    private SKBitmap? _originBackground;

    /// <summary>
    /// 是否原来的背景，即充当静态层的界面是无效的
    /// </summary>
    private bool _isOriginBackgroundDisable = false;

    /// <summary>
    /// 静态笔迹层
    /// </summary>
    public List<SkiaStrokeSynchronizer> StaticInkInfoList { get; } = new List<SkiaStrokeSynchronizer>();

    private Dictionary<int, DrawStrokeContext> CurrentInputDictionary { get; } =
        new Dictionary<int, DrawStrokeContext>();

    public SKColor Color { set; get; } = SKColors.Red;

    public event EventHandler<SkiaStrokeSynchronizer>? StrokesCollected;

    //public IEnumerable<string> CurrentInkStrokePathEnumerable =>
    //    CurrentInputDictionary.Values.Select(t => t.InkStrokePath).Where(t => t != null).Select(t => t!.ToSvgPathData());


    public void DrawStrokeDown(InkingModeInputArgs args)
    {
        var context = new DrawStrokeContext(InkId.NewId(), args, Color, 20);
        CurrentInputDictionary[args.Id] = context;

        context.AllStylusPoints.Add(args.StylusPoint);
        context.TipStylusPoints.Enqueue(args.StylusPoint);
    }

    public void DrawStrokeMove(InkingModeInputArgs args)
    {
        if (CurrentInputDictionary.TryGetValue(args.Id, out var context))
        {
            context.AllStylusPoints.Add(args.StylusPoint);
            context.TipStylusPoints.Enqueue(args.StylusPoint);

            context.InkStrokePath?.Dispose();

            var outlinePointList = SimpleInkRender.GetOutlinePointList(context.AllStylusPoints.ToArray(), context.InkThickness);

            var skPath = new SKPath();
            skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());

            context.InkStrokePath = skPath;

            DrawAllInk();

            // 计算脏范围，用于渲染更新
            var additionSize = 100d; // 用于设置比简单计算的范围更大一点的范围，解决重采样之后的模糊
            var (x, y) = args.StylusPoint.Point;

            RenderBoundsChanged?.Invoke(this,
                new Rect(x - additionSize / 2, y - additionSize / 2, additionSize, additionSize));
        }
    }

    public void DrawAllInk()
    {
        var skCanvas = _skCanvas;
        skCanvas.Clear(Settings.ClearColor);

        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 0;
        skPaint.IsAntialias = true;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Fill;

        foreach (var drawStrokeContext in CurrentInputDictionary)
        {
            skPaint.Color = drawStrokeContext.Value.StrokeColor;

            if (drawStrokeContext.Value.InkStrokePath is { } path)
            {
                skCanvas.DrawPath(path, skPaint);
            }
        }

        foreach (var skiaStrokeSynchronizer in StaticInkInfoList)
        {
            DrawInk(skCanvas, skPaint, skiaStrokeSynchronizer);
        }

        skCanvas.Flush();
    }

    private static void DrawInk(SKCanvas skCanvas, SKPaint skPaint, SkiaStrokeSynchronizer inkInfo)
    {
        skPaint.Color = inkInfo.StrokeColor;

        if (inkInfo.InkStrokePath is { } path)
        {
            skCanvas.DrawPath(path, skPaint);
        }
    }

    public void DrawStrokeUp(InkingModeInputArgs args)
    {
        if (CurrentInputDictionary.Remove(args.Id, out var context))
        {
            context.IsUp = true;

            StaticInkInfoList.Add(new SkiaStrokeSynchronizer((uint) args.Id, context.InkId, context.StrokeColor, context.InkThickness, context.InkStrokePath, context.AllStylusPoints));
        }
    }

    /// <summary>
    /// 绘制使用的上下文信息
    /// </summary>
    class DrawStrokeContext : IDisposable
    {
        /// <summary>
        /// 绘制使用的上下文信息
        /// </summary>
        public DrawStrokeContext(InkId inkId, InkingModeInputArgs modeInputArgs, SKColor strokeColor, double inkThickness)
        {
            InkId = inkId;
            InkThickness = inkThickness;
            StrokeColor = strokeColor;
            InputInfo = modeInputArgs;

            List<StylusPoint> historyDequeueList = [];
            TipStylusPoints = new InkingFixedQueue<StylusPoint>(MaxTipStylusCount, historyDequeueList);
            _historyDequeueList = historyDequeueList;
        }

        /// <summary>
        /// 笔迹的 Id 号，基本上每个笔迹都是不相同的。和输入的 Id 是不相同的，这是给每个 Stroke 一个的，不同的 Stroke 是不同的。除非有人能够一秒一条笔迹，写 60 多年才能重复
        /// </summary>
        public InkId InkId { get; }


        public double InkThickness { get; }

        public SKColor StrokeColor { get; }
        public InkingModeInputArgs InputInfo { set; get; }


        /// <summary>
        /// 丢点的数量
        /// </summary>
        public int DropPointCount { set; get; }

        /// <summary>
        /// 笔尖的点
        /// </summary>
        public InkingFixedQueue<StylusPoint> TipStylusPoints { get; }

        public List<StylusPoint> AllStylusPoints { get; } = new List<StylusPoint>();

        /// <summary>
        /// 存放笔迹的笔尖的点丢出来的点
        /// </summary>
        private List<StylusPoint>? _historyDequeueList;

        /// <summary>
        /// 整个笔迹的点，包括笔尖的点
        /// </summary>
        public List<StylusPoint> GetAllStylusPointsOnFinish()
        {
            if (_historyDequeueList is null)
            {
                // 为了减少 List 对象的申请，这里将复用 _historyDequeueList 的 List 对象。这就导致了一旦上层调用过此方法，将不能重复调用，否则将会炸掉逻辑
                throw new InvalidOperationException("此方法只能在完成的时候调用一次，禁止多次调用");
            }

            // 将笔尖的点合并到 _historyDequeueList 里面，这样就可以一次性返回所有的点。减少创建一个比较大的数组。缺点是这么做将不能多次调用，否则数据将会不正确
            var historyDequeueList = _historyDequeueList;
            //historyDequeueList.AddRange(TipStylusPoints);
            int count = TipStylusPoints.Count; // 为什么需要取出来？因为会越出队越小
            for (int i = 0; i < count; i++)
            {
                // 全部出队列，即可确保数据全取出来
                TipStylusPoints.Dequeue();
            }

            // 防止被多次调用
            _historyDequeueList = null;
            return historyDequeueList;
        }

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

    /// <summary>
    /// 取多少个点做笔尖
    /// </summary>
    /// 经验值，原本只是想取 5 + 1 个点，但是发现这样笔尖太短了，于是再加一个点
    private const int MaxTipStylusCount = 7;


    #region IInputProcessor

    void IInkingInputProcessor.InputStart()
    {
        StaticDebugLogger.WriteLine("==========InputStart============");

        // 这是浅拷贝
        //_originBackground = SkBitmap?.Copy();

        UpdateOriginBackground();
    }

    void IInkingInputProcessor.Down(InkingModeInputArgs info)
    {
        var inkId = CreateInkId();
        var drawStrokeContext = new DrawStrokeContext(inkId, info, Settings.Color, Settings.InkThickness);
        CurrentInputDictionary.Add(info.Id, drawStrokeContext);

        StaticDebugLogger.WriteLine($"Down {info.Position.X:0.00},{info.Position.Y:0.00} CurrentInputDictionaryCount={CurrentInputDictionary.Count}");
        //_outputMove = false;
        _moveCount = 0;

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
        else
        {
            // 笔模式
            if (Settings.ShouldDrawStrokeOnDown)
            {
                var result = DrawStroke(drawStrokeContext, out _);
                // 必定不会成功，但是将点进行收集
                System.Diagnostics.Debug.Assert(!result);
            }
        }
    }

    //private bool _outputMove;

    private StepCounter _stepCounter = new StepCounter();
    private int _moveCount = 0;

    void IInkingInputProcessor.Move(InkingModeInputArgs info)
    {
        if (!CurrentInputDictionary.ContainsKey(info.Id))
        {
            // 如果丢失按下，那就不能画
            // 解决鼠标在其他窗口按下，然后移动到当前窗口
            StaticDebugLogger.WriteLine($"Lost Input Id={info.Id}");
            return;
        }

        if (_moveCount == 0)
        {
            //_stepCounter.Start();
        }
        _stepCounter.Record($"StartMove{_moveCount}");

        //if (!_outputMove)
        //{
        //    StaticDebugLogger.WriteLine($"IInputProcessor.Move {info.Position.X:0.00},{info.Position.Y:0.00}");
        //}

        //_outputMove = true;

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

            var stylusPointList = context.InputInfo.StylusPointList;
            if (stylusPointList != null)
            {
                // 这里是一个补丁的实现，因为现在底层没有处理多点计算的功能
                // 如果底层能够在 DrawStroke 处理多点，预计性能比当前好非常多
                // 至少可以减少重复清空和拼接创建路径

                // 大部分情况下都不会进入此分支，除非是卡了

                // 先执行丢点算法，避免进入太多的点
                var result = new List<StylusPoint>();
                result.Add(stylusPointList[0]);
                // 这里使用新的丢点的算法
                // 可以丢掉更多的点
                for (var i = 1; i < stylusPointList.Count; i++)
                {
                    var lastPoint = result[^1];
                    var currentPoint = stylusPointList[i];
                    var length = Math.Pow((lastPoint.Point.X - currentPoint.Point.X), 2)
                                 + Math.Pow((lastPoint.Point.Y - currentPoint.Point.Y), 2);
                    if (length < 4)
                    {
                        // 太近了
                        continue;
                    }

                    if (length < 10)
                    {
                        // 太近了
                        continue;
                    }
                    if (result.Count < 2)
                    {

                    }

                    result.Add(currentPoint);
                }

                _stepCounter.Record("完成丢点");

                StaticDebugLogger.WriteLine($"完成丢点 丢点数量：{stylusPointList.Count - result.Count}  实际参与绘制点数：{result.Count}");

                Rect currentRect = new Rect();
                bool isFirst = true;

                float pressure = stylusPoint.Pressure;
                double width = stylusPoint.Width ?? 0;
                double height = stylusPoint.Height ?? 0;

                foreach (var point in result)
                {
                    pressure = point.IsPressureEnable ? point.Pressure : pressure;
                    width = point.Width ?? width;
                    height = point.Height ?? height;

                    context.InputInfo = context.InputInfo with
                    {
                        StylusPoint = point with
                        {
                            Pressure = pressure,
                            Width = width,
                            Height = height,
                        }
                    };

                    if (DrawStroke(context, out var rect))
                    {
                        if (isFirst)
                        {
                            currentRect = rect;
                        }
                        else
                        {
                            currentRect = currentRect.Union(rect);
                        }
                    }

                    isFirst = false;
                }
                _stepCounter.Record("完成绘制");

                RenderBoundsChanged?.Invoke(this, currentRect);
            }
            else
            {
                if (DrawStroke(context, out var rect))
                {
                    RenderBoundsChanged?.Invoke(this, rect);
                }
            }
        }

        _stepCounter.Record($"EndMove{_moveCount}");

        _stepCounter.OutputToConsole();
        //_stepCounter.Restart();
    }

    void IInkingInputProcessor.Hover(InkingModeInputArgs args)
    {
        // 没有什么作用
    }

    void IInkingInputProcessor.Up(InkingModeInputArgs info)
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

        // 获取所有的点，包括笔尖的点
        var allStylusPoints = context.GetAllStylusPointsOnFinish();

        context.DropPointCount = 0;
        // 清空笔尖的点，属于多余逻辑，因为在 GetAllStylusPointsOnFinish 里已经包含清空笔尖逻辑。反正多做一次也没损失，还能让逻辑更加清晰
        context.TipStylusPoints.Clear();
        context.IsUp = true;

        var strokesCollectionInfo = new SkiaStrokeSynchronizer((uint) info.Id, context.InkId, context.StrokeColor, context.InkThickness, context.InkStrokePath, allStylusPoints);
        StaticInkInfoList.Add(strokesCollectionInfo);
        StrokesCollected?.Invoke(this, strokesCollectionInfo);

        //if (CurrentInputDictionary.All(t => t.Value.IsUp))
        //{
        //    //完成等待清理
        //}
    }

    void IInkingInputProcessor.InputComplete()
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
    void IInkingInputProcessor.Leave()
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

    private DrawStrokeContext UpdateInkingStylusPoint(InkingModeInputArgs info)
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
            // 理论上不会进入此分支
            StaticDebugLogger.WriteLine($"UpdateInkingStylusPoint 找不到笔迹点");
            var inkId = CreateInkId();
            context = new DrawStrokeContext(inkId, info, Settings.Color, Settings.InkThickness);
            CurrentInputDictionary.Add(info.Id, context);
            return context;
        }
    }

    private int _currentInkId;

    private InkId CreateInkId()
    {
        var currentInkId = _currentInkId;
        _currentInkId++;
        return new InkId(currentInkId); // return _currentInkId++ 的意思，只是这个可读性太垃圾了
    }

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
        if (Settings.DropPointSettings.DropPointStrategy == DropPointStrategy.Aggressive)
        {
            // 激进策略，会丢很多点
            if (pointList.Count < 2)
            {
                return false;
            }

            var aPoint = pointList[^2].Point;
            var bPoint = pointList[^1].Point;
            var cPoint = currentStylusPoint.Point;

            // 短路代码，如果 b 和 c 点相同，那就直接返回可丢掉
            if (bPoint == cPoint)
            {
                return true;
            }

            // 如果 b 和 c 距离足够长，那就不能替换
            const int maxSquareDistance = (2 * 2 + 1);/*实际长度超过2的长度*/
            if (SquareDistanceTo(bPoint, cPoint) > maxSquareDistance)
            {
                return false;
            }
            // 同理，如果 a 和 c 的距离足够长，也不能替换
            if (SquareDistanceTo(aPoint, cPoint) > maxSquareDistance)
            {
                return false;
            }

            const double minDistance = 1;

            var segment2D = new Segment2D(new Point2D(aPoint.X, aPoint.Y), new Point2D(cPoint.X, cPoint.Y));
            var point2D = new Point2D(bPoint.X, bPoint.Y);
            var distance = segment2D.GetDistanceToLine(point2D);
            if (distance < minDistance)
            {
                return true;
            }

            return false;

            static double SquareDistanceTo(Point a, Point b)
            {
                var x = a.X - b.X;
                var y = a.Y - b.Y;
                return x * x + y * y;
            }
        }
        else
        {
            // 普通的丢点，不会丢太多
            if (pointList.Count < 3)
            {
                return false;
            }

            // 已经丢了10个点了，就不继续丢点了
            if (dropPointCount >= Settings.DropPointSettings.MaxDropPointCount)
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
    }

    private bool DrawStroke(DrawStrokeContext context, out Rect drawRect)
    {
        //StaticDebugLogger.WriteLine($"DrawStroke {context.InputInfo.StylusPoint.Point}");
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

                // 当前点能丢，则不绘制
                if (Settings.DynamicRenderType == InkCanvasDynamicRenderTipStrokeType.RenderAllTouchingStrokeWithClip)
                {
                    return false;
                }
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


    public static unsafe bool ReplacePixels(uint* destinationBitmap, uint* sourceBitmap, SKRectI destinationRectI,
        SKRectI sourceRectI, uint destinationPixelWidthLengthOfUint, uint sourcePixelWidthLengthOfUint)
    {
        if (destinationRectI.Width != sourceRectI.Width || destinationRectI.Height != sourceRectI.Height)
        {
            return false;
        }

        //for(var sourceRow = sourceRectI.Top; sourceRow< sourceRectI.Bottom; sourceRow++)
        //{
        //    for (var sourceColumn = sourceRectI.Left; sourceColumn < sourceRectI.Right; sourceColumn++)
        //    {
        //        var sourceIndex = sourceRow * sourceRectI.Width + sourceColumn;

        //        var destinationRow = destinationRectI.Top + sourceRow - sourceRectI.Top;
        //        var destinationColumn = destinationRectI.Left + sourceColumn - sourceRectI.Left;
        //        var destinationIndex = destinationRow * destinationRectI.Width + destinationColumn;

        //        destinationBitmap[destinationIndex] = sourceBitmap[sourceIndex];
        //    }
        //}

        for (var sourceRow = sourceRectI.Top; sourceRow < sourceRectI.Bottom; sourceRow++)
        {
            var sourceStartColumn = sourceRectI.Left;
            var sourceStartIndex = sourceRow * destinationPixelWidthLengthOfUint + sourceStartColumn;

            var destinationRow = destinationRectI.Top + sourceRow - sourceRectI.Top;
            var destinationStartColumn = destinationRectI.Left;
            var destinationStartIndex = destinationRow * sourcePixelWidthLengthOfUint + destinationStartColumn;

            Unsafe.CopyBlockUnaligned((destinationBitmap + destinationStartIndex), (sourceBitmap + sourceStartIndex), (uint) (destinationRectI.Width * sizeof(uint)));

            //for (var sourceColumn = sourceRectI.Left; sourceColumn < sourceRectI.Right; sourceColumn++)
            //{
            //    var sourceIndex = sourceRow * destinationPixelWidthLengthOfUint + sourceColumn;

            //    var destinationColumn = destinationRectI.Left + sourceColumn - sourceRectI.Left;
            //    var destinationIndex = destinationRow * sourcePixelWidthLengthOfUint + destinationColumn;

            //    destinationBitmap[destinationIndex] = sourceBitmap[sourceIndex];
            //}
        }

        return true;
    }

    [MemberNotNull(nameof(_originBackground))]
    private unsafe void UpdateOriginBackground()
    {
        // 需要使用 SKCanvas 才能实现拷贝
        _originBackground ??= new SKBitmap(new SKImageInfo(ApplicationDrawingSkBitmap.Width,
            ApplicationDrawingSkBitmap.Height, ApplicationDrawingSkBitmap.ColorType,
            ApplicationDrawingSkBitmap.AlphaType,
            ApplicationDrawingSkBitmap.ColorSpace), SKBitmapAllocFlags.None);
        _isOriginBackgroundDisable = false;

        //using var skCanvas = new SKCanvas(_originBackground);
        //skCanvas.Clear();
        //skCanvas.DrawBitmap(ApplicationDrawingSkBitmap, 0, 0);
        var applicationPixelHandler = ApplicationDrawingSkBitmap.GetPixels(out var length);
        var originBackgroundPixelHandler = _originBackground.GetPixels();
        Unsafe.CopyBlock((void*) originBackgroundPixelHandler, (void*) applicationPixelHandler, (uint) length);
    }
}


#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;

using BujeeberehemnaNurgacolarje;

using Microsoft.Maui.Graphics;

using SkiaSharp;

namespace ReewheaberekaiNayweelehe;

partial record InkingInputInfo(int Id, StylusPoint StylusPoint, ulong Timestamp)
{
    public bool IsMouse { init; get; }
};

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
partial record InkInfo(int Id);
=======
>>>>>>> 06026b0bbb703276589096b11fd69181ce02f21c

=======
>>>>>>> fc149583aa0a4eb1ed4aa8ca82c20621a7b49d41
/// <summary>
/// 画板的配置
/// </summary>
/// <param name="EnableClippingEraser">是否允许使用裁剪方式的橡皮擦，而不是走静态笔迹层</param>
/// <param name="AutoSoftPen">是否开启自动软笔模式</param>
record SkInkCanvasSettings(bool EnableClippingEraser = true, bool AutoSoftPen = true)
=======
enum InputMode
>>>>>>> 9994af5e3facc399bf93df657e69c36f21288956
{
    Ink,
    Manipulate,
}

class InkingInputManager
{
    public InkingInputManager(SkInkCanvas skInkCanvas)
    {
        SkInkCanvas = skInkCanvas;
    }

    public SkInkCanvas SkInkCanvas { get; }

    public InputMode InputMode { set; get; } = InputMode.Manipulate;

    private int _downCount;

    private StylusPoint _lastStylusPoint;

    public void Down(InkingInputInfo info)
    {
        _downCount++;
        if (_downCount > 2)
        {
            InputMode = InputMode.Manipulate;
        }

        if (InputMode == InputMode.Ink)
        {
            SkInkCanvas.DrawStrokeDown(info);
        }
        else if (InputMode == InputMode.Manipulate)
        {
            _lastStylusPoint = info.StylusPoint;
        }
    }

    public void Move(InkingInputInfo info)
    {
        if (InputMode == InputMode.Ink)
        {
            SkInkCanvas.DrawStrokeMove(info);
        }
        else if (InputMode == InputMode.Manipulate)
        {
            SkInkCanvas.ManipulateMove(new Point(info.StylusPoint.Point.X - _lastStylusPoint.Point.X, info.StylusPoint.Point.Y - _lastStylusPoint.Point.Y));

            _lastStylusPoint = info.StylusPoint;
        }
    }

    public void Up(InkingInputInfo info)
    {
        if (InputMode == InputMode.Ink)
        {
            SkInkCanvas.DrawStrokeUp(info);
        }
        else if (InputMode == InputMode.Manipulate)
        {
            SkInkCanvas.ManipulateMove(new Point(info.StylusPoint.Point.X - _lastStylusPoint.Point.X, info.StylusPoint.Point.Y - _lastStylusPoint.Point.Y));
            SkInkCanvas.ManipulateFinish();

            _lastStylusPoint = info.StylusPoint;
        }
    }
}

partial class SkInkCanvas
{
    public SkInkCanvas(SKCanvas skCanvas, SKBitmap applicationDrawingSkBitmap)
    {
        _skCanvas = skCanvas;
        ApplicationDrawingSkBitmap = applicationDrawingSkBitmap;

        RenderSplashScreen();
    }

    public void RenderSplashScreen()
    {
        if (_skCanvas is null || ApplicationDrawingSkBitmap is null)
        {
            // 理论上不可能进入这里
            return;
        }

        _skCanvas.Clear(SKColors.White);

        using var skPaint = new SKPaint();
        skPaint.Color = SKColors.Black;
        skPaint.StrokeWidth = 2;
        skPaint.Style = SKPaintStyle.Stroke;

        for (int y = 0; y < ApplicationDrawingSkBitmap.Height * 2; y += 25)
        {
            //_skCanvas.DrawLine(0, y, ApplicationDrawingSkBitmap.Width, y, skPaint);

            var color = new SKColor((uint) Random.Shared.Next()).WithAlpha((byte) Random.Shared.Next(100, 0xFF));

            var inkPointList = new List<StylusPoint>();
            for (int i = 0; i < ApplicationDrawingSkBitmap.Width * 2; i++)
            {
                inkPointList.Add(new StylusPoint(i, y));
            }

            AddInk(color, inkPointList);
        }

        for (int x = 0; x < ApplicationDrawingSkBitmap.Width * 2; x += 25)
        {
            var color = new SKColor((uint) Random.Shared.Next()).WithAlpha((byte) Random.Shared.Next(100, 0xFF));

            var inkPointList = new List<StylusPoint>();
            for (int i = 0; i < ApplicationDrawingSkBitmap.Height * 2; i++)
            {
                inkPointList.Add(new StylusPoint(x, i));
            }

            AddInk(color, inkPointList);
        }

        DrawAllInk();

        void AddInk(SKColor color, IList<StylusPoint> inkPointList)
        {
            var drawStrokeContext = new DrawStrokeContext(new InkingInputInfo(Random.Shared.Next(), new StylusPoint(), (ulong) Environment.TickCount64), color);
            drawStrokeContext.AllStylusPoints.AddRange(inkPointList);

            var outline = SimpleInkRender.GetOutlinePointList([.. inkPointList], 10);
            var skPath = new SKPath();
            skPath.AddPoly(outline.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());

            drawStrokeContext.InkStrokePath = skPath;

            StaticInkInfoList.Add(new InkInfo(Random.Shared.Next(), drawStrokeContext));
        }
    }

    public event EventHandler<Rect>? RenderBoundsChanged;

    private SKCanvas? _skCanvas;

    /// <summary>
    /// 原应用输出的内容
    /// </summary>
    public SKBitmap? ApplicationDrawingSkBitmap { set; get; }

    record InkInfo(int Id, DrawStrokeContext Context);

    /// <summary>
    /// 静态笔迹层
    /// </summary>
    private List<InkInfo> StaticInkInfoList { get; } = new List<InkInfo>();

    private Dictionary<int, DrawStrokeContext> CurrentInputDictionary { get; } =
        new Dictionary<int, DrawStrokeContext>();

    public SKColor Color { set; get; } = SKColors.Red;

    public void DrawStrokeDown(InkingInputInfo info)
    {
        var context = new DrawStrokeContext(info, Color);
        CurrentInputDictionary[info.Id] = context;

        context.AllStylusPoints.Add(info.StylusPoint);
        context.TipStylusPoints.Enqueue(info.StylusPoint);
    }

    public void DrawStrokeMove(InkingInputInfo info)
    {
        if (CurrentInputDictionary.TryGetValue(info.Id, out var context))
        {
            context.AllStylusPoints.Add(info.StylusPoint);
            context.TipStylusPoints.Enqueue(info.StylusPoint);

            context.InkStrokePath?.Dispose();

            var outlinePointList = SimpleInkRender.GetOutlinePointList(context.AllStylusPoints.ToArray(), 20);

            var skPath = new SKPath();
            skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());

            context.InkStrokePath = skPath;

            DrawAllInk();

            // 计算脏范围，用于渲染更新
            var additionSize = 100d; // 用于设置比简单计算的范围更大一点的范围，解决重采样之后的模糊
            var (x, y) = info.StylusPoint.Point;

            RenderBoundsChanged?.Invoke(this,
                new Rect(x - additionSize / 2, y - additionSize / 2, additionSize, additionSize));
        }
    }

    private void DrawAllInk()
    {
        if (_skCanvas is null)
        {
            // 理论上不可能进入这里
            return;
        }

        var skCanvas = _skCanvas;
        skCanvas.Clear();

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

        foreach (var inkInfo in StaticInkInfoList)
        {
            DrawInk(skCanvas, skPaint, inkInfo);
        }

        skCanvas.Flush();
    }

    private static void DrawInk(SKCanvas skCanvas, SKPaint skPaint, InkInfo inkInfo)
    {
        skPaint.Color = inkInfo.Context.StrokeColor;

        if (inkInfo.Context.InkStrokePath is { } path)
        {
            skCanvas.DrawPath(path, skPaint);
        }
    }

    public void DrawStrokeUp(InkingInputInfo info)
    {
        if (CurrentInputDictionary.Remove(info.Id, out var context))
        {
            context.IsUp = true;

            StaticInkInfoList.Add(new InkInfo(info.Id, context));
        }
    }

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

        /// <summary>
        /// 整个笔迹的点，包括笔尖的点
        /// </summary>
        public List<StylusPoint> AllStylusPoints { get; } = new List<StylusPoint>();

        public SKPath? InkStrokePath { set; get; }

        public bool IsUp { set; get; }

        public void Dispose()
        {
            //InkStrokePath?.Dispose();
        }
    }

    /// <summary>
    /// 取多少个点做笔尖
    /// </summary>
    private const int MaxTipStylusCount = 7;
<<<<<<< HEAD
<<<<<<< HEAD
}

partial class SkInkCanvas
{
    public SkInkCanvas()
    {
    }

    public SkInkCanvasSettings Settings { get; set; } = new SkInkCanvasSettings();

    public void SetCanvas(SKCanvas canvas)
    {
        _skCanvas = canvas;
    }

<<<<<<< HEAD
    public SKCanvas? SkCanvas => _skCanvas;
    private SKCanvas? _skCanvas;
=======
>>>>>>> 423d0d697db86f6b4bda17c5b4e51fb5d4a99877


    /// <summary>
    /// 原来的背景
    /// </summary>
    private SKBitmap? _originBackground;

    private bool _isOriginBackgroundDisable = false;

    //public SKSurface? SkSurface { set; get; }

<<<<<<< HEAD
    public event EventHandler<Rect>? RenderBoundsChanged;
    public void RaiseRenderBoundsChanged(Rect rect) => RenderBoundsChanged?.Invoke(this, rect);
=======
>>>>>>> 5f60ba247fc4ed21a521355bcad673325d210fa3


<<<<<<< HEAD
    public IEnumerable<SKPath> CurrentInkStrokePathEnumerable => CurrentInputDictionary.Values.Select(t => t.InkStrokePath)
        .Where(t => t != null)!;

    /// <summary>
    /// 取多少个点做笔尖
    /// </summary>
    private const int MaxTipStylusCount = 7;
=======
>>>>>>> 06026b0bbb703276589096b11fd69181ce02f21c


    private readonly StylusPoint[] _cache = new StylusPoint[MaxTipStylusCount + 1];

    private int MainInputId { set; get; }

    private void InputStart()
    {
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
=======

    #region 漫游

    /// <summary>
    /// 漫游完成，需要将内容重新使用路径绘制，保持清晰
    /// </summary>
    public void ManipulateFinish()
    {
        if (_skCanvas is null)
        {
            // 理论上不可能进入这里
            return;
        }

        var skCanvas = _skCanvas;
        skCanvas.Clear();

        skCanvas.Save();
        skCanvas.SetMatrix(_totalMatrix);

        DrawAllInk();

        skCanvas.Restore();
    }

    public void ManipulateMove(Point delta)
    {
        //_totalMatrix = _totalMatrix * SKMatrix.CreateTranslation((float) delta.X, (float) delta.Y);
        var translation = SKMatrix.CreateTranslation((float) delta.X, (float) delta.Y);
        _totalMatrix = SKMatrix.Concat(_totalMatrix, translation);

        // 像素漫游的方法
        MoveWithPixel(delta);

        //// 几何漫游的方法
        //MoveWithPath(delta);
    }

    private SKMatrix _totalMatrix = SKMatrix.CreateIdentity();

    private unsafe void MoveWithPixel(Point delta)
    {
        if (_skCanvas is null)
        {
            // 理论上不可能进入这里
            return;
        }

        if (ApplicationDrawingSkBitmap is null)
        {
            // 理论上不可能进入这里
            return;
        }

        //"xx".StartsWith("x", StringComparison.Ordinal)

        var pixels = ApplicationDrawingSkBitmap.GetPixels(out var length);

        var pixelLengthOfUint = length / 4;
        if (_cachePixel is null || _cachePixel.Length != pixelLengthOfUint)
        {
            _cachePixel = new uint[pixelLengthOfUint];
        }

        fixed (uint* pCachePixel = _cachePixel)
        {
            //var byteCount = (uint) length * sizeof(uint);
            ////Buffer.MemoryCopy((uint*) pixels, pCachePixel, byteCount, byteCount);
            //////Buffer.MemoryCopy((uint*) pixels, pCachePixel, 0, byteCount);
            //for (int i = 0; i < length; i++)
            //{
            //    var pixel = ((uint*) pixels)[i];
            //    pCachePixel[i] = pixel;
            //}

            var byteCount = (uint) length;
            Unsafe.CopyBlock(pCachePixel, (uint*) pixels, byteCount);
        }

        int destinationX, destinationY, destinationWidth, destinationHeight;
        int sourceX, sourceY, sourceWidth, sourceHeight;
        if (delta.X > 0)
        {
            destinationX = (int) delta.X;
            destinationWidth = ApplicationDrawingSkBitmap.Width - destinationX;
            sourceX = 0;
        }
        else
        {
            destinationX = 0;
            destinationWidth = ApplicationDrawingSkBitmap.Width - ((int) -delta.X);

            sourceX = (int) -delta.X;
        }

        if (delta.Y > 0)
        {
            destinationY = (int) delta.Y;
            destinationHeight = ApplicationDrawingSkBitmap.Height - destinationY;
            sourceY = 0;
        }
        else
        {
            destinationY = 0;
            destinationHeight = ApplicationDrawingSkBitmap.Height - (int) -delta.Y;

            sourceY = ((int) -delta.Y);
        }

        sourceWidth = destinationWidth;
        sourceHeight = destinationHeight;

        SKRectI destinationRectI = SKRectI.Create(destinationX, destinationY, destinationWidth, destinationHeight);
        SKRectI sourceRectI = SKRectI.Create(sourceX, sourceY, sourceWidth, sourceHeight);

        // 计算脏范围，用于在此绘制笔迹
        var topRectI = SKRectI.Create(0, 0, ApplicationDrawingSkBitmap.Width, destinationY);
        var bottomRectI = SKRectI.Create(0, destinationY + destinationHeight, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height - destinationY - destinationHeight);
        var leftRectI = SKRectI.Create(0, destinationY, destinationX, destinationHeight);
        var rightRectI = SKRectI.Create(destinationX + destinationWidth, destinationY, ApplicationDrawingSkBitmap.Width - destinationX - destinationWidth, destinationHeight);

        var hitInk = new List<InkInfo>();
        foreach (var skRectI in (Span<SKRectI>) [topRectI, bottomRectI, leftRectI, rightRectI])
        {
            HitInk(skRectI);
        }

        //var skCanvas = _skCanvas;
        //skCanvas.Clear();
        //foreach (var skRectI in (Span<SKRectI>) [topRectI, bottomRectI, leftRectI, rightRectI])
        //{
        //    using var skPaint = new SKPaint();
        //    skPaint.StrokeWidth = 0;
        //    skPaint.IsAntialias = true;
        //    skPaint.FilterQuality = SKFilterQuality.High;
        //    skPaint.Style = SKPaintStyle.Fill;
        //    skPaint.Color = SKColors.Blue;
        //    var skRect = SKRect.Create(skRectI.Left, skRectI.Top, skRectI.Width, skRectI.Height);

        //    skCanvas.DrawRect(skRect, skPaint);
        //}
        //skCanvas.Flush();

        var skCanvas = _skCanvas;
        skCanvas.Clear();
        skCanvas.Save();
        skCanvas.SetMatrix(_totalMatrix);
        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 0;
        skPaint.IsAntialias = true;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Fill;

        foreach (var inkInfo in hitInk.Distinct())
        {
            DrawInk(skCanvas, skPaint, inkInfo);
        }

        skCanvas.Restore();
        skCanvas.Flush();

        fixed (uint* pCachePixel = _cachePixel)
        {
            var pixelLength = (uint) (ApplicationDrawingSkBitmap.Width);

            ReplacePixels((uint*) pixels, pCachePixel, destinationRectI, sourceRectI, pixelLength, pixelLength);
        }

        RenderBoundsChanged?.Invoke(this,
            new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));

        static bool IsEmptySize(SKRectI skRectI) => skRectI.Width == 0 || skRectI.Height == 0;

        void HitInk(SKRectI skRectI)
        {
            var skRect = SKRect.Create(skRectI.Left, skRectI.Top, skRectI.Width, skRectI.Height);
            foreach (var inkInfo in StaticInkInfoList)
            {
                if (inkInfo.Context.InkStrokePath is { } path)
                {
                    var bounds = path.Bounds;
                    if (skRect.IntersectsWith(bounds))
                    {
                        hitInk.Add(inkInfo);
                    }
                }
            }
        }
    }

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

    private uint[]? _cachePixel;

    private void MoveWithPath(Point delta)
    {
        _totalTransform = new Point(_totalTransform.X + delta.X, _totalTransform.Y + delta.Y);
>>>>>>> 9994af5e3facc399bf93df657e69c36f21288956

        if (_skCanvas is null)
        {
            // 理论上不可能进入这里
<<<<<<< HEAD
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

        return false;
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

        if (Settings.EnableClippingEraser)
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

                    var skRectDropPoint = new SKRect(xDropPoint, yDropPoint, (float) (xDropPoint + width), (float) (yDropPoint + height));

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
            EraserView.DrawEraserView(canvas, 30, 45);
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
=======
>>>>>>> fc149583aa0a4eb1ed4aa8ca82c20621a7b49d41
=======
            return;
        }

        if (ApplicationDrawingSkBitmap is null)
        {
            // 理论上不可能进入这里
            return;
        }

        var skCanvas = _skCanvas;
        skCanvas.Clear();

        skCanvas.Save();

        skCanvas.Translate((float) _totalTransform.X, (float) _totalTransform.Y);

        DrawAllInk();

        skCanvas.Restore();

        RenderBoundsChanged?.Invoke(this,
            new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));
    }

    private Point _totalTransform;

    #endregion
>>>>>>> 9994af5e3facc399bf93df657e69c36f21288956
}
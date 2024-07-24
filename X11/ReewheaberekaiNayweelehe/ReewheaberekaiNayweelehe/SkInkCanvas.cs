#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using BujeeberehemnaNurgacolarje;

using Microsoft.Maui.Graphics;

using SkiaInkCore;
using SkiaInkCore.Interactives;
using SkiaInkCore.Primitive;

using SkiaSharp;

namespace ReewheaberekaiNayweelehe;

partial class SkInkCanvas
{
    public SkInkCanvas(SKCanvas skCanvas, SKBitmap applicationDrawingSkBitmap)
    {
        _skCanvas = skCanvas;
        ApplicationDrawingSkBitmap = applicationDrawingSkBitmap;
    }

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

    public void DrawStrokeDown(InkingModeInputArgs args)
    {
        var context = new DrawStrokeContext(new InkId(), args, Color, 20);
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
            ModeInputArgs = modeInputArgs;

            //List<StylusPoint> historyDequeueList = [];
            //TipStylusPoints = new InkingFixedQueue<StylusPoint>(MaxTipStylusCount, historyDequeueList);
            //_historyDequeueList = historyDequeueList;
            TipStylusPoints = new FixedQueue<StylusPoint>(MaxTipStylusCount);
        }

        /// <summary>
        /// 笔迹的 Id 号，基本上每个笔迹都是不相同的。和输入的 Id 是不相同的，这是给每个 Stroke 一个的，不同的 Stroke 是不同的。除非有人能够一秒一条笔迹，写 60 多年才能重复
        /// </summary>
        public InkId InkId { get; }


        public double InkThickness { get; }

        public SKColor StrokeColor { get; }
        public InkingModeInputArgs ModeInputArgs { set; get; }

        /// <summary>
        /// 丢点的数量
        /// </summary>
        public int DropPointCount { set; get; }

        /// <summary>
        /// 笔尖的点
        /// </summary>
        public FixedQueue<StylusPoint> TipStylusPoints { get; }

        public List<StylusPoint> AllStylusPoints { get; } = new List<StylusPoint>();

        ///// <summary>
        ///// 存放笔迹的笔尖的点丢出来的点
        ///// </summary>
        //private List<StylusPoint>? _historyDequeueList;

        ///// <summary>
        ///// 整个笔迹的点，包括笔尖的点
        ///// </summary>
        //public List<StylusPoint> GetAllStylusPointsOnFinish()
        //{
        //    if (_historyDequeueList is null)
        //    {
        //        // 为了减少 List 对象的申请，这里将复用 _historyDequeueList 的 List 对象。这就导致了一旦上层调用过此方法，将不能重复调用，否则将会炸掉逻辑
        //        throw new InvalidOperationException("此方法只能在完成的时候调用一次，禁止多次调用");
        //    }

        //    // 将笔尖的点合并到 _historyDequeueList 里面，这样就可以一次性返回所有的点。减少创建一个比较大的数组。缺点是这么做将不能多次调用，否则数据将会不正确
        //    var historyDequeueList = _historyDequeueList;
        //    //historyDequeueList.AddRange(TipStylusPoints);
        //    int count = TipStylusPoints.Count; // 为什么需要取出来？因为会越出队越小
        //    for (int i = 0; i < count; i++)
        //    {
        //        // 全部出队列，即可确保数据全取出来
        //        TipStylusPoints.Dequeue();
        //    }

        //    // 防止被多次调用
        //    _historyDequeueList = null;
        //    return historyDequeueList;
        //}

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

partial class SkInkCanvas
{
    // 漫游相关

    /// <summary>
    /// 漫游完成，需要将内容重新使用路径绘制，保持清晰
    /// </summary>
    public void ManipulateFinish()
    {
        var skCanvas = _skCanvas;
        skCanvas.Clear();

        skCanvas.Save();
        skCanvas.SetMatrix(_totalMatrix);

        DrawAllInk();

        skCanvas.Restore();
        _isOriginBackgroundDisable = true;
    }

    public void ManipulateScale(ScaleContext scale)
    {
        var scaleMatrix = SKMatrix.CreateScale(scale.X, scale.Y, scale.PivotX, scale.PivotY);
        _totalMatrix = SKMatrix.Concat(_totalMatrix, scaleMatrix);

        var skCanvas = _skCanvas;
        skCanvas.Clear();

        skCanvas.Save();
        skCanvas.SetMatrix(_totalMatrix);

        DrawAllInk();

        skCanvas.Restore();

        _isOriginBackgroundDisable = true;
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

        _isOriginBackgroundDisable = true;
    }

    private SKMatrix _totalMatrix = SKMatrix.CreateIdentity();

    private unsafe void MoveWithPixel(Point delta)
    {
        var pixels = ApplicationDrawingSkBitmap.GetPixels(out var length);

        UpdateOriginBackground();

        //var pixelLengthOfUint = length / 4;
        //if (_cachePixel is null || _cachePixel.Length != pixelLengthOfUint)
        //{
        //    _cachePixel = new uint[pixelLengthOfUint];
        //}

        //fixed (uint* pCachePixel = _cachePixel)
        //{
        //    //var byteCount = (uint) length * sizeof(uint);
        //    ////Buffer.MemoryCopy((uint*) pixels, pCachePixel, byteCount, byteCount);
        //    //////Buffer.MemoryCopy((uint*) pixels, pCachePixel, 0, byteCount);
        //    //for (int i = 0; i < length; i++)
        //    //{
        //    //    var pixel = ((uint*) pixels)[i];
        //    //    pCachePixel[i] = pixel;
        //    //}

        //    var byteCount = (uint) length;
        //    Unsafe.CopyBlock(pCachePixel, (uint*) pixels, byteCount);
        //}

        int destinationX, destinationY, destinationWidth, destinationHeight;
        int sourceX, sourceY, sourceWidth, sourceHeight;

        if (delta.X > 0)
        {
            delta.X += 20;

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

            sourceY = (int) -delta.Y;
        }

        sourceWidth = destinationWidth;
        sourceHeight = destinationHeight;

        SKRectI destinationRectI = SKRectI.Create(destinationX, destinationY, destinationWidth, destinationHeight);
        SKRectI sourceRectI = SKRectI.Create(sourceX, sourceY, sourceWidth, sourceHeight);

        // 计算脏范围，用于在此绘制笔迹
        var topRect = SKRect.Create(0, 0, ApplicationDrawingSkBitmap.Width, destinationY);
        var bottomRect = SKRect.Create(0, destinationY + destinationHeight, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height - destinationY - destinationHeight);
        var leftRect = SKRect.Create(0, destinationY, destinationX, destinationHeight);
        var rightRect = SKRect.Create(destinationX + destinationWidth, destinationY, ApplicationDrawingSkBitmap.Width - destinationX - destinationWidth, destinationHeight);

        var hitRectList = new List<SKRect>(4);
        var matrix = _totalMatrix.Invert();
        Span<SKRect> hitRectSpan = [topRect, bottomRect, leftRect, rightRect];
        foreach (var skRect in hitRectSpan)
        {
            if (!IsEmptySize(skRect))
            {
                hitRectList.Add(matrix.MapRect(skRect));
            }
        }

        var hitInk = new List<SkiaStrokeSynchronizer>();
        foreach (var skiaStrokeSynchronizer in StaticInkInfoList)
        {
            foreach (var skRect in hitRectList)
            {
                if (IsHit(skiaStrokeSynchronizer, skRect))
                {
                    hitInk.Add(skiaStrokeSynchronizer);
                    break;
                }
            }
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

        foreach (var skiaStrokeSynchronizer in hitInk)
        {
            DrawInk(skCanvas, skPaint, skiaStrokeSynchronizer);
        }

        skCanvas.Restore();
        skCanvas.Flush();

        var cachePixel = _originBackground.GetPixels();
        uint* pCachePixel = (uint*) cachePixel;
        var pixelLength = (uint) (ApplicationDrawingSkBitmap.Width);

        ReplacePixels((uint*) pixels, pCachePixel, destinationRectI, sourceRectI, pixelLength, pixelLength);

        RenderBoundsChanged?.Invoke(this,
            new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));

        static bool IsEmptySize(SKRect skRect) => skRect.Width == 0 || skRect.Height == 0;

        static bool IsHit(SkiaStrokeSynchronizer inkInfo, SKRect skRect)
        {
            if (inkInfo.InkStrokePath is { } path)
            {
                var bounds = path.Bounds;
                if (skRect.IntersectsWith(bounds))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void MoveWithPath(Point delta)
    {
        _totalTransform = new Point(_totalTransform.X + delta.X, _totalTransform.Y + delta.Y);

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
}
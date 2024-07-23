#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;

using BujeeberehemnaNurgacolarje;

using Microsoft.Maui.Graphics;

using SkiaSharp;

namespace ReewheaberekaiNayweelehe;

record InkingInputInfo(int Id, StylusPoint StylusPoint, ulong Timestamp)
{
    public bool IsMouse { init; get; }
};

enum InputMode
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
            for (int i = 0; i < ApplicationDrawingSkBitmap.Width; i++)
            {
                inkPointList.Add(new StylusPoint(i, y));
            }

            AddInk(color, inkPointList);
        }

        for (int x = 0; x < ApplicationDrawingSkBitmap.Width * 2; x += 25)
        {
            var color = new SKColor((uint) Random.Shared.Next()).WithAlpha((byte) Random.Shared.Next(100, 0xFF));

            var inkPointList = new List<StylusPoint>();
            for (int i = 0; i < ApplicationDrawingSkBitmap.Height; i++)
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
        skPaint.StrokeWidth = 0.1f;
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
            skPaint.Color = inkInfo.Context.StrokeColor;

            if (inkInfo.Context.InkStrokePath is { } path)
            {
                skCanvas.DrawPath(path, skPaint);
            }
        }

        skCanvas.Flush();
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

        fixed (uint* pCachePixel = _cachePixel)
        {
            var pixelLength = (uint) (ApplicationDrawingSkBitmap.Width);

            ReplacePixels((uint*) pixels, pCachePixel, destinationRectI, sourceRectI, pixelLength, pixelLength);
        }

        RenderBoundsChanged?.Invoke(this,
            new Rect(0, 0, ApplicationDrawingSkBitmap.Width, ApplicationDrawingSkBitmap.Height));
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
}
#nullable enable
using System.Diagnostics;
using BujeeberehemnaNurgacolarje;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace ReewheaberekaiNayweelehe;

record InkingInputInfo(int Id, StylusPoint StylusPoint, ulong Timestamp)
{
    public bool IsMouse { init; get; }
};

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

partial class SkInkCanvas
{
    public SkInkCanvas(SKCanvas skCanvas, SKBitmap applicationDrawingSkBitmap)
    {
        _skCanvas = skCanvas;
        ApplicationDrawingSkBitmap = applicationDrawingSkBitmap;
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
            skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float)t.X, (float)t.Y)).ToArray());

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
}
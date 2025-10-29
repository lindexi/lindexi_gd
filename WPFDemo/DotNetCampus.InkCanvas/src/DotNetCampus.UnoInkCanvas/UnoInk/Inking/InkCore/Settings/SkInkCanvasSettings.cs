using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace UnoInk.Inking.InkCore.Settings;

/// <summary>
/// 画板的配置
/// </summary>
/// <param name="AutoSoftPen">是否开启自动软笔模式</param>
public record SkInkCanvasSettings(bool AutoSoftPen = true)
{
    public InkCanvasEraserAlgorithmMode EraserMode { init; get; } =
        InkCanvasEraserAlgorithmMode.EnableClippingEraserWithoutEraserPathCombine;

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

    /// <summary>
    /// 笔迹颜色
    /// </summary>
    public SKColor Color { get; init; } = SKColors.Red;

    /// <summary>
    /// 笔迹粗细
    /// </summary>
    public double InkThickness { get; init; } = 20;

    /// <summary>
    /// 橡皮擦尺寸，可以在业务层，在手势橡皮擦过程中更改
    /// </summary>
    public Size EraserSize { get; init; } = DefaultEraserSize;

    /// <summary>
    /// 将触摸尺寸当成橡皮擦尺寸，即橡皮擦大小不完全跟随 <see cref="EraserSize"/> 尺寸，而是会根据 <see cref="StylusPoint"/> 的触摸大小决定
    /// </summary>
    public bool EnableStylusSizeAsEraserSize { get; init; } = true;

    /// <summary>
    /// 橡皮擦是否可以一直按照触摸尺寸修改橡皮擦尺寸。属于演示效果较好，实际使用效果差。仅当 <see cref="EnableStylusSizeAsEraserSize"/> 为 true 时此属性才有效。为 false 时，将在超过 <see cref="EraserCanResizeDuringTimeSpan"/> 时间，设置为最后的触摸面积固定大小，即只允许在开始擦的时候根据触摸面积修改大小，之后将固定大小
    /// </summary>
    public bool CanEraserAlwaysFollowsTouchSize { init; get; } = false;

    /// <summary>
    /// 橡皮擦可以根据触摸面积尺寸修改橡皮擦大小的时间。如果 <see cref="CanEraserAlwaysFollowsTouchSize"/> 为 true 则此属性无效。仅当 <see cref="EnableStylusSizeAsEraserSize"/> 为 true 时此属性才有效
    /// </summary>
    public TimeSpan EraserCanResizeDuringTimeSpan { init; get; } = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// 默认的橡皮擦尺寸
    /// </summary>
    /// 在 Paint DefaultEraserSize 是 48x72 大小
    public static Size DefaultEraserSize => new Size(30, 45);

    /// <summary>
    /// 是否锁定最小橡皮擦尺寸，即要求橡皮擦尺寸最小为 <see cref="MinEraserSize"/> 大小
    /// </summary>
    public bool LockMinEraserSize { init; get; } = true;

    /// <summary>
    /// 最小橡皮擦尺寸。仅当 <see cref="LockMinEraserSize"/> 为 true 时生效
    /// </summary>
    public Size MinEraserSize { init; get; } = new Size(48, 72);

    /// <summary>
    /// 橡皮擦丢点时间，在这个时间内的连续输入将会被丢掉
    /// </summary>
    public TimeSpan EraserDropPointTimeSpan { get; init; } = TimeSpan.FromMilliseconds(20);

    /// <summary>
    /// 最小的橡皮擦手势尺寸，用于判断是否进入手势模式
    /// </summary>
    /// 和 <see cref="MinEraserGesturePhysicalSizeCm"/> 不同的是，此属性是像素单位
    public Size MinEraserGesturePixelSize { get; init; } = new Size(30, 45);

    /// <summary>
    /// 最小的橡皮擦手势尺寸，物理尺寸，单位厘米
    /// </summary>
    /// 据说大家的手都是 6 厘米，也不知道是谁说的
    public double MinEraserGesturePhysicalSizeCm { get; init; } = 6;

    /// <summary>
    /// 在开始输入多久之后，就不能再进入橡皮擦了
    /// </summary>
    /// 这是一个弱约定，上层业务方可取此属性判断，也可以强行进入手势橡皮擦模式
    public TimeSpan DisableEnterEraserGestureAfterInputDuring { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 是否启用手势橡皮擦 默认不启用，由上层业务自己调用进入手势橡皮擦模式。因为在这一层不好进行计算
    /// 此属性设置为 false 之后，需要上层业务自行决定什么时机进入手势橡皮擦模式，通过调用 EnterEraserMode 方法进入手势橡皮擦模式
    /// 此属性设置为 true 将会在框架层，通过输入的 <see cref="StylusPoint"/> 的触摸尺寸，通过像素判断方法，判断是否大于 <see cref="MinEraserGesturePixelSize"/> 尺寸决定是否进入橡皮擦模式。由于通过像素方式判断不靠谱，因此推荐不要开启此属性。业务层自己决定更好
    /// </summary>
    public bool EnableEraserGesture { get; init; } = false;

    /// <summary>
    /// 是否忽略压感。
    /// </summary>
    public bool IgnorePressure { get; init; } = true;

    /// <summary>
    /// 是否在按下时需要调用 DrawStroke 方法，用于解决丢失按下的点
    /// </summary>
    public bool ShouldDrawStrokeOnDown { get; init; } = false;

    /// <summary>
    /// 清空笔迹的配置
    /// </summary>
    public CleanStrokeSettings CleanStrokeSettings { get; init; } = new CleanStrokeSettings();
}

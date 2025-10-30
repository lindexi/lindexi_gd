using Avalonia;

using DotNetCampus.Inking.StrokeRenderers;

using SkiaSharp;

namespace DotNetCampus.Inking.Contexts;
class AvaSkiaInkCanvasSettings
{
    /// <summary>
    /// 笔迹粗细。
    /// </summary>
    public float InkThickness { get; set; } = DefaultInkThickness;

    public static float DefaultInkThickness => 10;

    /// <summary>
    /// 笔迹颜色。
    /// </summary>
    public SKColor InkColor { get; set; } = DefaultInkColor;

    public static SKColor DefaultInkColor => SKColors.Red;

    /// <summary>
    /// 橡皮擦尺寸，可以在业务层，在手势橡皮擦过程中更改
    /// </summary>
    public Size EraserSize { get; set; } = DefaultEraserSize;

    /// <summary>
    /// 默认的橡皮擦尺寸
    /// </summary>
    public static Size DefaultEraserSize => new Size(48,
        72);

    /// <summary>
    /// 将触摸尺寸当成橡皮擦尺寸，即橡皮擦大小不完全跟随 <see cref="EraserSize"/> 尺寸，而是会根据 <see cref="DotNetCampus.Inking.Primitive.InkStylusPoint"/> 的触摸大小决定
    /// </summary>
    public bool EnableStylusSizeAsEraserSize { get; set; } = true;

    /// <summary>
    /// 橡皮擦是否可以一直按照触摸尺寸修改橡皮擦尺寸。属于演示效果较好，实际使用效果差。仅当 <see cref="EnableStylusSizeAsEraserSize"/> 为 true 时此属性才有效。为 false 时，将在超过 <see cref="EraserCanResizeDuringTimeSpan"/> 时间，设置为最后的触摸面积固定大小，即只允许在开始擦的时候根据触摸面积修改大小，之后将固定大小
    /// </summary>
    public bool CanEraserAlwaysFollowsTouchSize { set; get; } = false;

    /// <summary>
    /// 是否允许使用位图缓存在合适的时候替代真实的笔迹以提升部分场景下的笔迹性能。
    /// </summary>
    /// <remarks>
    /// 如果指定为 <see langword="true"/>，则书写模块会在合适的时机切换真实的笔迹和位图缓存，可能可以提高性能。<br/>
    /// 如果指定为 <see langword="false"/>，则位图缓存将不会工作，将一直使用真实的笔迹渲染。
    /// </remarks>
    public bool IsBitmapCacheEnabled { get; set; } = true;

    /// <summary>
    /// 橡皮擦可以根据触摸面积尺寸修改橡皮擦大小的时间。如果 <see cref="CanEraserAlwaysFollowsTouchSize"/> 为 true 则此属性无效。仅当 <see cref="EnableStylusSizeAsEraserSize"/> 为 true 时此属性才有效
    /// </summary>
    public TimeSpan EraserCanResizeDuringTimeSpan { set; get; } = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// 是否锁定最小橡皮擦尺寸，即要求橡皮擦尺寸最小为 <see cref="MinEraserSize"/> 大小
    /// </summary>
    public bool LockMinEraserSize { init; get; } = true;

    /// <summary>
    /// 最小橡皮擦尺寸。仅当 <see cref="LockMinEraserSize"/> 为 true 时生效
    /// </summary>
    public Size MinEraserSize { init; get; } = DefaultEraserSize;

    /// <summary>
    /// 最大橡皮擦尺寸。理论上用不着，只是用来限制尺寸而已
    /// </summary>
    public Size MaxEraserSize { init; get; } = new Size(600, 600);

    /// <summary>
    /// 当使用位图缓存（<see cref="IsBitmapCacheEnabled"/> 为 <see langword="true"/>）时，最大的位图缓存大小。单位为像素。
    /// </summary>
    public int MaxBitmapCacheSize { get; set; } =
        // 兆芯上似乎 1920×1080 都扛不住？？？
        OperatingSystem.IsLinux() ? 1080 * 1080 :
        // 主流 Intel/AMD 上目前看性能还行。
        OperatingSystem.IsWindows() ? 2560 * 1440 :
        // 默认随便给个值吧。
        1920 * 1080;

    /// <summary>
    /// 是否需要重新创建笔迹点，采用平滑滤波算法
    /// </summary>
    public bool ShouldReCreatePoint { get; set; }

    /// <summary>
    /// 笔迹渲染器
    /// </summary>
    public ISkiaInkStrokeRenderer? InkStrokeRenderer { get; set; }

    /// <summary>
    /// 设置或获取是否需要忽略压感
    /// </summary>
    public bool IgnorePressure { get; set; }
}
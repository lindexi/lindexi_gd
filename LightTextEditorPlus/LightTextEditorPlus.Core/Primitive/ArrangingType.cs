namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
///     布局方式
/// </summary>
public enum ArrangingType
{
    /// <summary>
    ///     横排布局
    /// </summary>
    /// 即 horizontal writing mode
    Horizontal,

    /// <summary>
    ///     竖排布局。也称 直排
    /// </summary>
    /// 即 vertical writing mode
    Vertical,

    /// <summary>
    ///     蒙古文布局，从右到左
    /// </summary>
    Mongolian
}

public class TextArrangingType
{
    private TextArrangingType()
    {
    }

    /// <summary>
    ///     横排布局
    /// </summary>
    /// 即 horizontal writing mode
    public static TextArrangingType Horizontal
        => _horizontal ??= new TextArrangingType();

    private static TextArrangingType? _horizontal;

    /// <summary>
    ///     竖排布局。也称 直排
    /// </summary>
    /// 即 vertical writing mode
    public static TextArrangingType Vertical => _vertical ??= new TextArrangingType
    {
        IsVertical = true,
        IsLeftToRightVertical = true,
    };

    private static TextArrangingType? _vertical;

    /// <summary>
    ///     蒙古文布局，从右到左
    /// </summary>
    public static TextArrangingType Mongolian => _mongolian ??= new TextArrangingType
    {
        IsVertical = true,
        // 蒙古文布局，从右到左
        IsLeftToRightVertical = false,
    };

    private static TextArrangingType? _mongolian;

    public bool IsHorizontal => !IsVertical;

    public bool IsVertical { get; private init; }

    public bool IsLeftToRightVertical { get; private init; }

    /// <summary>
    /// 拉丁文字是否需要旋转，旋转角度为是多少，都是90度倍数为主
    /// </summary>
    public int LatinRotationDegree { get; private init; }

    /// <summary>
    /// 数字是否需要旋转
    /// </summary>
    public bool NumberRotationDegree { get; private init; }

    /// <summary>
    /// 默认旋转角度 
    /// </summary>
    public const int DefaultRotationDegree = 90;
}
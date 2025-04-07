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
    public static TextArrangingType Horizontal => new TextArrangingType();
    public static TextArrangingType Vertical => new TextArrangingType
    {
        IsVertical = true,
        IsLeftToRightVertical = true,
    };

    public static TextArrangingType Mongolian => new TextArrangingType
    {
        IsVertical = true,
        // 蒙古文布局，从右到左
        IsLeftToRightVertical = false,
    };

    private TextArrangingType()
    {
    }

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
}


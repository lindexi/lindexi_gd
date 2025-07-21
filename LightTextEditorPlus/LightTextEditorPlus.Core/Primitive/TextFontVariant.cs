using System;

using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 表示文本上下标字符属性
/// </summary>
/// 在 PPT 里面，使用 Baseline 表示上下标的距离。使用正数表示上标，使用负数表示下标，详细请看 059b7e5807c33ebc6e9156971e6b8d51235a9c0e
/// 在 Word 里面，采用 VerticalAlignment Value= "Subscript" 或 "Superscript" 来表示下标或上标
/// 采用 PPT 的定义方式，可以包含 Word 的功能。但 PPT 里面使用正数负数表示比较不直观，且判断逻辑稍微复杂，不如再添加一个枚举属性好了。加一个枚举只加一个 sizeof(byte) 长度，不亏
public readonly record struct TextFontVariant()
{
    /// <summary>
    /// 正常，非上下标
    /// </summary>
    public bool IsNormal => FontVariants == TextFontVariants.Normal;

    /// <summary>
    /// 上标或下标的基线比例
    /// </summary>
    /// 和 PPT 不同的是，不采用正负号来表示。需要配合表示上下标的属性来表示
    public double BaselineProportion { get; init; } = 0.3;

    /// <summary>
    /// 上下标
    /// </summary>
    public TextFontVariants FontVariants { get; init; } = TextFontVariants.Normal;

    /// <summary>
    /// 正常，非上下标
    /// </summary>
    public static TextFontVariant Normal => new TextFontVariant();

    /// <summary>
    /// 上标
    /// </summary>
    public static TextFontVariant Superscript => new TextFontVariant()
    {
        FontVariants = TextFontVariants.Superscript
    };

    /// <summary>
    /// 下标
    /// </summary>
    public static TextFontVariant Subscript => new TextFontVariant()
    {
        FontVariants = TextFontVariants.Subscript
    };

    /// <inheritdoc />
    public bool Equals(TextFontVariant other)
    {
        if (this.IsNormal)
        {
            if (other.IsNormal)
            {
                // 无视 BaselineProportion 属性判断，因为正常状态下不需要考虑上下标
                return true;
            }
            else
            {
                return false;
            }
        }

        return FontVariants == other.FontVariants && Nearly.Equals(BaselineProportion, other.BaselineProportion);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (FontVariants == TextFontVariants.Normal)
        {
            return 0;
        }

        return HashCode.Combine((int) FontVariants, BaselineProportion);
    }

    /// <summary>
    /// 从 <see cref="TextFontVariants"/> 转换为 <see cref="TextFontVariant"/>。
    /// </summary>
    /// <param name="textFontVariants"></param>
    public static implicit operator TextFontVariant(TextFontVariants textFontVariants)
    {
        return textFontVariants switch
        {
            TextFontVariants.Normal => Normal,
            TextFontVariants.Superscript => Superscript,
            TextFontVariants.Subscript => Subscript,
            _ => throw new ArgumentOutOfRangeException(nameof(textFontVariants), textFontVariants, null)
        };
    }
}

/// <summary>
/// 文本字体变体，上下标
/// </summary>
/// <remarks>
/// 文本字符属性里面用的是 <see cref="TextFontVariant"/> （名字上不带 s 的结构体）作为上下标属性。核心差别是在枚举基础上添加了 <see cref="TextFontVariant.BaselineProportion"/> 属性，允许设置基线比例
/// </remarks>
public enum TextFontVariants : byte
{
    /// <summary>
    /// 正常，非上下标
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 上标
    /// </summary>
    Superscript,

    /// <summary>
    /// 下标
    /// </summary>
    Subscript,
}
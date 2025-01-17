using System;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 按照比例对齐字符在行内的位置
/// <para>
/// 字符在行内的垂直对齐方式，如在顶部、比例值、底部。可使用本类型预设的配置，如 <see cref="TopAlignment"/> 、 <see cref="CenterAlignment"/>、 <see cref="BottomAlignment"/> 等。也可以手动创建传入比例值
/// </para>
/// <para>
/// 当一行有行距的时候，即行高远大于字高。此时字符可选在行内的对齐方式，如和 PPT 一样，是属于底部对齐，或和 Word 一样是属于顶部对齐，或和 WPF 一样比例值对齐
/// </para>
/// </summary>
/// 为什么不称为 Leading 呢？因为 Leading 有很多混淆的含义
/// 如在 GDI 的 TEXTMETRICA 里面，将 ExternalLeading 称为 应用程序在行之间添加的额外前导空间量。在其他一些定义里面，也会将 Leading 当成上加符号的空间。在 Skia 里定义成字体建议的两行之间的基线行距
/// 在 Direct Write 里面，将 DWRITE_TEXT_ALIGNMENT_LEADING 当成了水平的左对齐
///
/// 为什么不称为 BaselineRatioAlignment 或 BaselineAlignment 呢？根据 [BaselineAlignment Enum (System.Windows) Microsoft Learn](https://learn.microsoft.com/zh-cn/dotnet/api/system.windows.baselinealignment?view=windowsdesktop-9.0 ) 文档可以知道，BaselineAlignment 是作用在 TextRun 上的，而不是整个文本框的全局属性。整个文本框定义的是字符在行内的垂直对齐方式，属于一类样式
public readonly record struct RatioVerticalCharInLineAlignment
{
    /// <summary>
    /// 按照比例对齐字符在行内的位置
    /// </summary>
    public RatioVerticalCharInLineAlignment(double lineSpaceRatio = TextContext.LineSpaceRatio)
    {
        if (lineSpaceRatio is > 1 or < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineSpaceRatio), $"传入的行高比例只能是 0-1 范围");
        }

        LineSpaceRatio = lineSpaceRatio;
    }

    /// <summary>
    /// 行高比例
    /// </summary>
    public double LineSpaceRatio { get; }

    /// <summary>
    /// 获取顶部对齐的字符在行内的对齐方式
    /// </summary>
    public static RatioVerticalCharInLineAlignment TopAlignment => new RatioVerticalCharInLineAlignment(0);

    /// <summary>
    /// 获取居中对齐的字符在行内的对齐方式
    /// </summary>
    /// <returns></returns>
    public static RatioVerticalCharInLineAlignment CenterAlignment => new RatioVerticalCharInLineAlignment(0.5);

    /// <summary>
    /// 获取按照 1/2 行高对齐的字符在行内的对齐方式，完全等同于 <see cref="CenterAlignment"/> 居中对齐。此方法的存在仅仅只是为了告诉你，更加正确的是调用 <see cref="CenterAlignment"/> 方法而已
    /// </summary>
    /// <returns></returns>
    /// 为什么有 half leading 这个词？ 在 Pango 和 https://github.com/toptensoftware/RichTextKit 和 https://www.w3.org/TR/CSS21/visudet.html#leading 里面都有提到 half leading 的概念
    public static RatioVerticalCharInLineAlignment HalfLeadingAlignment => CenterAlignment;

    /// <summary>
    /// 获取底部对齐的字符在行内的对齐方式
    /// </summary>
    /// <returns></returns>
    public static RatioVerticalCharInLineAlignment BottomAlignment => new RatioVerticalCharInLineAlignment(1);

    /// <summary>
    /// 获取文本编辑器预设的行高比例对齐方式，以 4/5 方式分割
    /// </summary>
    public static RatioVerticalCharInLineAlignment TextEditorPresetRatioAlignment =>
        new RatioVerticalCharInLineAlignment(TextContext.LineSpaceRatio);
}

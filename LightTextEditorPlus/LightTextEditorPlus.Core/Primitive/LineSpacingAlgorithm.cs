namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 行距算法
/// </summary>
/// ReSharper disable InconsistentNaming
public enum LineSpacingAlgorithm
{
    /// <summary>
    /// 使用 WPF 的行距计算方法。使用此算法即可计算出 TextBlock 的行距
    /// <para>
    /// 算法 LineHeight = FontSize * FontFamilyLineSpacing * (LineSpace - 1) / 10 + FontSize * FontFamilyLineSpacing
    /// </para>
    /// <para>
    /// 以上算法的参数有 FontSize 字体大小和 FontFamilyLineSpacing 字体记录的行距参数和 LineSpace 用户设置的几倍行距
    /// </para>
    /// <para>
    /// 其中 FontFamilyLineSpacing 通过 FontFamily.LineSpacing 赋值
    /// </para>
    /// </summary>
    WPF = 0,

    /// <summary>
    /// 使用 PPT 的行距计算方法。可以计算出和 PPT 文本布局差不多的行距
    /// <para>
    /// 算法 LineHeight = PPTPixelLineSpacing = (PPTFL * LineSpace + b) * FontSize
    /// </para>
    /// <para>
    /// 以上 PPTFL 是 PPT Font Line Spacing 的意思，在 PPT 所有文字的行距都是这个值，无论实际用的是哪个字体，是一个常量
    /// </para>
    /// <para>
    /// PPTFL = 1.2018
    /// </para>
    /// <para>
    /// 而 LineSpace 是用户设置的几倍行距，例如 PPT 里设置的 1 倍行距或 2 倍行距等
    /// </para>
    /// <para>
    /// 以上的 b 也是一个常量
    /// </para>
    /// <para>
    /// b = 0.0034
    /// </para>
    /// 此算法计算出来的行距和 PPT 的相近
    /// </summary>
    PPT = 1,

    // todo Word 行距
    // 在 Word 里几倍行距就是几倍行高
}

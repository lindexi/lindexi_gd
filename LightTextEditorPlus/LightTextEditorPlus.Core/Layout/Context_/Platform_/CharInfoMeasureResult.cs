using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 字符测量结果
/// </summary>
/// <param name="Bounds">字符的范围，经常 XY 都是 0 0 的值</param>
/// <param name="Baseline">基线，相对于字符的左上角，字符坐标系。即无论这个字符放在哪一行哪一段，这个字符的基线都是一样的</param>
public readonly record struct CharInfoMeasureResult(TextRect Bounds, double Baseline)
{
}

/// <summary>
/// 测量字符信息参数
/// </summary>
/// <param name="CharData"></param>
/// <param name="RunList"></param>
/// <param name="CurrentIndex"></param>
/// <param name="Paragraph"></param>
/// <param name="UpdateLayoutContext"></param>
public readonly record struct CharMeasureArgument
(
    CharData CharData,
    TextReadOnlyListSpan<CharData> RunList,
    int CurrentIndex,
    ITextParagraph Paragraph,
    UpdateLayoutContext UpdateLayoutContext
)
{
    public CharInfo CharInfo => CharData.ToCharInfo();
}
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 测量行内字符参数
/// </summary>
/// <param name="RunList"></param>
/// <param name="CurrentIndex">当前字符的序号</param>
/// <param name="LineRemainingWidth">这一行剩余的宽度</param>
/// <param name="ParagraphProperty"></param>
public readonly record struct SingleCharInLineLayoutArgument(ReadOnlyListSpan<CharData> RunList, int CurrentIndex,
    double LineRemainingWidth, ParagraphProperty ParagraphProperty)
{
    public CharData CurrentCharData => RunList[CurrentIndex];
}
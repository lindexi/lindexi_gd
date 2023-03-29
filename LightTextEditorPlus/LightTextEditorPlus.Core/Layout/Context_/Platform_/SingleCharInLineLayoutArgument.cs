using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 测量行内字符参数
/// </summary>
/// <param name="RunList"></param>
/// <param name="CurrentIndex">当前字符的序号，相对于 <see cref="RunList"/> 的序号</param>
/// <param name="LineRemainingWidth">这一行剩余的宽度</param>
/// <param name="ParagraphProperty"></param>
public readonly record struct SingleCharInLineLayoutArgument(ReadOnlyListSpan<CharData> RunList, int CurrentIndex,
    double LineRemainingWidth, ParagraphProperty ParagraphProperty)
{
    /// <summary>
    /// 当前的字符信息
    /// </summary>
    public CharData CurrentCharData => RunList[CurrentIndex];
}
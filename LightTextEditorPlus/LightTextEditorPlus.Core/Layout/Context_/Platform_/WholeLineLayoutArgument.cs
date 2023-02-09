using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 段内行测量布局参数
/// </summary>
/// <param name="ParagraphProperty"></param>
/// <param name="CharDataList"></param>
/// <param name="LineMaxWidth">这一行能布局的最大宽度</param>
/// <param name="CurrentStartPoint">当前行的起始点，相对于文本框的坐标</param>
public readonly record struct WholeLineLayoutArgument(ParagraphProperty ParagraphProperty, in ReadOnlyListSpan<CharData> CharDataList, double LineMaxWidth, Point CurrentStartPoint)
{
    
}
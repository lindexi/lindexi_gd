using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 行渲染参数
/// </summary>
/// <param name="IsDrawn">这一行是否曾经画过了，如果画过了，可能可以从 <see cref="LineAssociatedRenderData"/> 获取到缓存的数据</param>
/// <param name="IsLineStartPointUpdated">是否行的起始点变更。例如横排文本的此行的前面输入新行，导致此行的 Y 坐标变更，此时需要重新更新此行渲染坐标</param>
/// <param name="LineAssociatedRenderData">上次绘制完成之后，传入给框架层的渲染缓存数据。通过 <see cref="LineDrawnResult.LineAssociatedRenderData"/> 传入给框架，原封不动在渲染时传给业务层</param>
/// <param name="StartPoint">行的起始点坐标</param>
/// <param name="CharList">行里面的字符信息</param>
public readonly record struct LineDrawingArgument(bool IsDrawn, bool IsLineStartPointUpdated,
    object? LineAssociatedRenderData, TextPoint StartPoint,  TextReadOnlyListSpan<CharData> CharList)
{
    /// <inheritdoc cref="LineLayoutData.LineContentSize"/>
    public TextSize LineContentSize { get; init; }

    /// <inheritdoc cref="LineLayoutData.LineCharTextSize"/>
    public TextSize LineCharTextSize { get; init; }
}

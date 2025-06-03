using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 水平布局坐标系下的文档布局范围
/// </summary>
public readonly struct DocumentLayoutBoundsInHorizontalArrangingCoordinateSystem()
{
    /// <summary>
    /// 相对于水平排列的坐标系的点，文档内容的起始点
    /// 对于水平布局来说，永远 X 是 0 的值。即使是进行水平居中等行为，也不会改变这个点的 X 坐标
    /// 其作用是在进行 Y 坐标的垂直居中、居下时，能够不动段落坐标，让段落自然快速完成布局
    /// </summary>
    public required TextPointInHorizontalArrangingCoordinateSystem DocumentContentStartPoint { get; init; }

    /// <summary>
    /// 文档内容的尺寸
    /// </summary>
    public required TextSize DocumentContentSize { get; init; }

    /// <summary>
    /// 文档外接范围的尺寸
    /// </summary>
    public required TextSize DocumentOutlineSize { get; init; }

    /// <summary>
    /// 文本框
    /// </summary>
    public required TextEditorCore TextEditor { get; init; }

    /// <summary>
    /// 转换为按照当前排版类型的坐标，如果是竖排，则转换为竖排坐标
    /// </summary>
    /// <returns></returns>
    public DocumentLayoutBounds ToDocumentLayoutBounds()
    {
        TextPoint documentContentStartPoint = DocumentContentStartPoint.ToCurrentArrangingTypePoint();
        TextSize documentContentSize = DocumentContentSize;
        TextSize documentOutlineSize = DocumentOutlineSize;

        if (TextEditor.ArrangingType.IsVertical)
        {
            documentContentSize = documentContentSize.SwapWidthAndHeight();
            documentOutlineSize = documentOutlineSize.SwapWidthAndHeight();
        }

        TextRect documentContentBounds = new TextRect(documentContentStartPoint, documentContentSize);
        TextRect documentOutlineBounds = new TextRect(TextPoint.Zero, documentOutlineSize);
        return new DocumentLayoutBounds(documentContentBounds,
            documentOutlineBounds);
    }
}
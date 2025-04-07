using System;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 文档布局范围
/// </summary>
/// <param name="DocumentContentBounds">内容范围，可能小于 DocumentWidth 和 DocumentHeight 的值。左上角会受到布局影响，如垂直居中时，左上角就会在中间偏上的地方</param>
/// <param name="DocumentOutlineBounds">外接范围。外接范围的左上角是 0,0 点</param>
public readonly record struct DocumentLayoutBounds(TextRect DocumentContentBounds, TextRect DocumentOutlineBounds)
{
}

public readonly struct DocumentLayoutBoundsInHorizontalArrangingCoordinateSystem()
{
    public required TextPointInHorizontalArrangingCoordinateSystem DocumentContentStartPoint { get; init; }
    public required TextSize DocumentContentSize { get; init; }
    public required TextSize DocumentOutlineSize { get; init; }

    public required TextEditorCore TextEditor { get; init; }

    public DocumentLayoutBounds ToDocumentLayoutBounds()
    {
        TextPoint documentContentStartPoint = DocumentContentStartPoint.ToCurrentArrangingTypePoint();
        TextSize documentContentSize = DocumentContentSize;
        TextSize documentOutlineSize = DocumentOutlineSize;

        if (TextEditor.ArrangingType != ArrangingType.Horizontal)
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
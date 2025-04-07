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

public readonly record struct DocumentLayoutBoundsInHorizontalArrangingCoordinateSystem(
    TextRect DocumentContentBounds,
    TextRect DocumentOutlineBounds,
    TextEditorCore TextEditor)
{
    public DocumentLayoutBounds ToDocumentLayoutBounds()
    {
        throw new NotImplementedException();
    }
}
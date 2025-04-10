using LightTextEditorPlus.Core.Primitive;

using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Layout;

class VerticalArrangingLayoutProvider : HorizontalArrangingLayoutProvider
{
    public VerticalArrangingLayoutProvider(LayoutManager layoutManager) : base(layoutManager)
    {
    }

    public override ArrangingType ArrangingType => ArrangingType.Vertical;

    protected override double GetLineMaxWidth()
    {
        double lineMaxWidth = TextEditor.SizeToContent switch
        {
            TextSizeToContent.Manual => TextEditor.DocumentManager.DocumentHeight,
            TextSizeToContent.Width => double.PositiveInfinity,
            TextSizeToContent.Height => TextEditor.DocumentManager.DocumentHeight,
            TextSizeToContent.WidthAndHeight => double.PositiveInfinity,
            _ => throw new ArgumentOutOfRangeException()
        };
        return lineMaxWidth;
    }

    protected override TextSize CalculateDocumentOutlineSize(in TextSize documentContentSize)
    {
        double lineMaxWidth = GetLineMaxWidth();
        var documentWidth = lineMaxWidth;
        if (!double.IsFinite(documentWidth))
        {
            // 非有限宽度，则采用文档的宽度
            documentWidth = documentContentSize.Width;
        }

        var documentHeight = TextEditor.DocumentManager.DocumentWidth;
        if (!double.IsFinite(documentHeight))
        {
            documentHeight = documentContentSize.Height;
        }

        return new TextSize(documentWidth, documentHeight);
    }
}
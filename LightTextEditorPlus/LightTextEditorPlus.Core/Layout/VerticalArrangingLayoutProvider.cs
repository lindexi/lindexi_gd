using LightTextEditorPlus.Core.Primitive;

using System;

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
            TextSizeToContent.Height => TextEditor.DocumentManager.DocumentWidth,
            TextSizeToContent.WidthAndHeight => double.PositiveInfinity,
            _ => throw new ArgumentOutOfRangeException()
        };
        return lineMaxWidth;
    }
}
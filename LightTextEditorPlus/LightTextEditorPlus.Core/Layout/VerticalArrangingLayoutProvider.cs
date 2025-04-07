using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

class VerticalArrangingLayoutProvider : HorizontalArrangingLayoutProvider
{
    public VerticalArrangingLayoutProvider(LayoutManager layoutManager) : base(layoutManager)
    {
    }

    public override ArrangingType ArrangingType => ArrangingType.Vertical;

}
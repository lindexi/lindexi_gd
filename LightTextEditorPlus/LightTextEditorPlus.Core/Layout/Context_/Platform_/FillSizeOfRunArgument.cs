using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Platform;

public readonly record struct FillSizeOfRunArgument(TextReadOnlyListSpan<CharData> RunList, UpdateLayoutContext UpdateLayoutContext)
{
    public CharData CurrentCharData => RunList[0];

    public ICharDataLayoutInfoSetter CharDataLayoutInfoSetter => UpdateLayoutContext;

    public void SetCurrentCharDataMeasureResult(in CharInfoMeasureResult result)
    {
        CharDataLayoutInfoSetter.SetCharDataInfo(CurrentCharData, result.Bounds.TextSize, result.Baseline);
    }
};
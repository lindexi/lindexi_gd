using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils;

internal interface IInternalCharDataSizeMeasurer
{
    TextSize GetCharSize(CharData charData);
}
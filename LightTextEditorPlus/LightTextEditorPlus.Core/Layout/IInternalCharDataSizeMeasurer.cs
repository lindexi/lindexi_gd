using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

internal interface IInternalCharDataSizeMeasurer
{
    TextSize GetCharSize(CharData charData);
}
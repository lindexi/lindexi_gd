using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

internal interface IInternalCharDataSizeMeasurer
{
    Size GetCharSize(CharData charData);
}
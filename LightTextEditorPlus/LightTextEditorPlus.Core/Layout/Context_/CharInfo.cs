using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Layout;

public readonly record struct CharInfo(ICharObject CharObject, IReadOnlyRunProperty RunProperty)
{
}
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

public readonly record struct SingleCharInLineLayoutResult(int TakeCharCount, Size Size)
{
    public bool CanTake => TakeCharCount > 0;
}
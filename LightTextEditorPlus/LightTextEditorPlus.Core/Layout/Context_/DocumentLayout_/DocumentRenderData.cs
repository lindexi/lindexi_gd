using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

class DocumentRenderData
{
    public bool IsDirty { set; get; }

    public Rect DocumentBounds { set; get; }
}
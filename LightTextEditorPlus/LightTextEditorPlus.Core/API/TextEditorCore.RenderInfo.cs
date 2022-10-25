using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core;

public partial class TextEditorCore
{
    public Rect GetDocumentBounds()
    {
        if (_layoutManager.DocumentRenderData.IsDirty)
        {
            throw new TextEditorDirtyException();
        }

        return _layoutManager.DocumentRenderData.DocumentBounds;
    }
}


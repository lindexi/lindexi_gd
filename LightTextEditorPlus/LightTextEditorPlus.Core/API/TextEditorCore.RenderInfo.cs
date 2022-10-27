using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core;

public partial class TextEditorCore
{
    /// <summary>
    /// 获取文档的布局尺寸，实际布局尺寸。此方法必须在文本布局完成之后才能调用
    /// </summary>
    /// <returns></returns>
    /// <exception cref="TextEditorDirtyException"></exception>
    public Rect GetDocumentLayoutBounds()
    {
        if (_layoutManager.DocumentRenderData.IsDirty)
        {
            throw new TextEditorDirtyException();
        }

        return _layoutManager.DocumentRenderData.DocumentBounds;
    }
}


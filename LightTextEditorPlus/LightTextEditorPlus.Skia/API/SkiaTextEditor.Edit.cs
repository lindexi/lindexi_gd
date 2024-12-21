using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Document;

using SkiaSharp;

namespace LightTextEditorPlus;

// 此文件存放编辑相关的方法
[APIConstraint("TextEditor.Edit.Input.txt")]
partial class SkiaTextEditor
{
    #region 输入

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.AppendText"/>
    public void AppendText(string text)
    {
        TextEditorCore.AppendText(text);
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.AppendRun"/>
    public void AppendRun(SkiaTextRun textRun)
    {
        TextEditorCore.AppendRun(textRun);
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.EditAndReplace"/>
    public void EditAndReplace(string text, Selection? selection = null)
    {
        TextEditorCore.EditAndReplace(text, selection);
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.EditAndReplaceRun"/>
    public void EditAndReplaceRun(SkiaTextRun textRun, Selection? selection = null)
    {
        TextEditorCore.EditAndReplaceRun(textRun, selection);
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Backspace"/>
    public void Backspace()
    {
        TextEditorCore.Backspace();
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Delete"/>
    public void Delete()
    {
        TextEditorCore.Delete();
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Remove"/>
    public void Remove(in Selection selection)
    {
        TextEditorCore.Remove(in selection);
    }

    #endregion
}

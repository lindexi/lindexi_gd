using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Document;

using SkiaSharp;

namespace LightTextEditorPlus;

// 此文件存放编辑相关的方法
[APIConstraint("TextEditor.Edit.Input.txt")]
partial class SkiaTextEditor
{
    #region 输入

    public void AppendText(string text)
    {
        TextEditorCore.AppendText(text);
    }

    public void AppendRun(SkiaTextRun textRun)
    {
        TextEditorCore.AppendRun(textRun);
    }

    public void EditAndReplace(string text, Selection? selection = null)
    {
        TextEditorCore.EditAndReplace(text, selection);
    }

    public void EditAndReplaceRun(SkiaTextRun textRun, Selection? selection = null)
    {
        TextEditorCore.EditAndReplaceRun(textRun, selection);
    }

    public void Backspace()
    {
        TextEditorCore.Backspace();
    }

    public void Delete()
    {
        TextEditorCore.Delete();
    }

    public void Remove(in Selection selection)
    {
        TextEditorCore.Remove(in selection);
    }

    #endregion
}
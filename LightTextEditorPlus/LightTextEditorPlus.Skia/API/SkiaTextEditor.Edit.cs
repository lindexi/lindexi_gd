using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Document;
using System;

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
        ArgumentNullException.ThrowIfNull(textRun);
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

    /// <summary>
    /// 根据原始文本中的 UTF-16 索引创建文档偏移量。
    /// 自动处理代理对字符（如 emoji）和 \r\n 折叠。
    /// </summary>
    /// <param name="text">原始文本（使用 UTF-16 编码）。</param>
    /// <param name="utf16Index">UTF-16 索引，即 string[index] 的 index。</param>
    /// <returns>对应的文档字符偏移。</returns>
    public DocumentOffset CreateDocumentOffsetFromUtf16Index(string text, int utf16Index)
        => TextEditorCore.CreateDocumentOffsetFromUtf16Index(text, utf16Index);

    /// <summary>
    /// 根据原始文本中的 UTF-16 索引创建文档偏移量。
    /// 自动处理代理对字符（如 emoji）和 \r\n 折叠。
    /// </summary>
    /// <param name="text">原始文本（使用 UTF-16 编码）的跨度。</param>
    /// <param name="utf16Index">UTF-16 索引，即 text[index] 的 index。</param>
    /// <returns>对应的文档字符偏移。</returns>
    public DocumentOffset CreateDocumentOffsetFromUtf16Index(ReadOnlySpan<char> text, int utf16Index)
        => TextEditorCore.CreateDocumentOffsetFromUtf16Index(text, utf16Index);

    #endregion
}

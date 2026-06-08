using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using System.Text;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core;

/// <summary>
/// 文本编辑扩展方法
/// </summary>
public static class TextEditorCoreTextExtensions
{
    /// <summary>
    /// 获取整个文本编辑器的文本
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    public static string GetText(this TextEditorCore textEditor)
    {
        return GetText(textEditor, textEditor.DocumentManager.GetAllDocumentSelection());
    }

    /// <summary>
    /// 获取文本
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="selection"></param>
    /// <returns></returns>
    public static string GetText(this TextEditorCore textEditor, in Selection selection)
    {
        return GetText(textEditor, new StringBuilder(), in selection).ToString();
    }

    /// <summary>
    /// 获取整个文本编辑器的文本
    /// </summary>
    public static StringBuilder GetText(this TextEditorCore textEditor, StringBuilder stringBuilder) => GetText(textEditor, stringBuilder, textEditor.DocumentManager.GetAllDocumentSelection());

    /// <summary>
    /// 获取文本
    /// </summary>
    public static StringBuilder GetText(this TextEditorCore textEditor, StringBuilder stringBuilder,
        in Selection selection)
    {
        foreach (CharData charData in textEditor.DocumentManager.GetCharDataRange(in selection))
        {
            charData.CharObject.CodePoint.AppendToStringBuilder(stringBuilder);
        }

        return stringBuilder;
    }

    /// <summary>
    /// 获取富文本
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="selection"></param>
    /// <returns></returns>
    public static IImmutableRunList GetRunList(this TextEditorCore textEditor, in Selection selection)
    {
        return textEditor.DocumentManager.GetImmutableRunList(in selection);
    }

    /// <summary>
    /// 根据原始文本中的 UTF-16 索引创建文档偏移量。
    /// 自动处理代理对字符（如 emoji）和 \r\n 折叠。
    /// </summary>
    /// <param name="textEditorCore">文本编辑器核心。</param>
    /// <param name="text">原始文本（使用 UTF-16 编码）。</param>
    /// <param name="utf16Index">UTF-16 索引，即 string[index] 的 index。</param>
    /// <returns>对应的文档字符偏移。</returns>
    public static DocumentOffset CreateDocumentOffsetFromUtf16Index(
        this TextEditorCore textEditorCore, string text, int utf16Index)
    {
        _ = textEditorCore;
        return DocumentOffset.FromUtf16Index(text, utf16Index);
    }
}
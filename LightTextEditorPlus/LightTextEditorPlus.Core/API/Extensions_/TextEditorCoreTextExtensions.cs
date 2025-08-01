using LightTextEditorPlus.Core.Document;
using System.Text;
using LightTextEditorPlus.Core.Carets;

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
}
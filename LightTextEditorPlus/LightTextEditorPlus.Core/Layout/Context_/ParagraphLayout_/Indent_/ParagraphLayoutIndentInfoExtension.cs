using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Layout;

static class ParagraphLayoutIndentInfoExtension
{
    /// <summary>
    /// 调试下确保段落属性的缩进信息和段落属性一致
    /// </summary>
    /// <param name="indentInfo"></param>
    /// <param name="paragraphProperty"></param>
    /// <exception cref="TextEditorInnerDebugException"></exception>
    public static void DebugVerifyParagraphPropertyIndentInfo(this ParagraphLayoutIndentInfo indentInfo,
        ParagraphProperty paragraphProperty)
    {
        EqualAssets(paragraphProperty.LeftIndentation, indentInfo.LeftIndentation, nameof(paragraphProperty.LeftIndentation));
        EqualAssets(paragraphProperty.RightIndentation, indentInfo.RightIndentation,
            nameof(paragraphProperty.RightIndentation));
        EqualAssets(paragraphProperty.Indent, indentInfo.Indent, nameof(paragraphProperty.Indent));
        if (paragraphProperty.IndentType != indentInfo.IndentType)
        {
            throw new TextEditorInnerDebugException($"对 IndentType 的预期和实际值不符。预期：{paragraphProperty.IndentType}，实际：{indentInfo.IndentType}");
        }

        static void EqualAssets(double expect, double actual, string name)
        {
            TextEditorInnerDebugAsset.AreEquals(expect, actual, name);
        }
    }
}
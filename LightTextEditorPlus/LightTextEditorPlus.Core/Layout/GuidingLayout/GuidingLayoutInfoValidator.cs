using System.Linq;

using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Layout;

internal static class GuidingLayoutInfoValidator
{
    public static bool TryValidate(TextEditorCore textEditor, GuidingLayoutInfo guidingLayoutInfo, out string message)
    {
        if (guidingLayoutInfo.ArrangingType != textEditor.ArrangingType)
        {
            message = $"指导布局的 ArrangingType={guidingLayoutInfo.ArrangingType} 与当前文本的 ArrangingType={textEditor.ArrangingType} 不匹配";
            return false;
        }

        var paragraphList = textEditor.DocumentManager.ParagraphManager.GetParagraphList();
        if (guidingLayoutInfo.ParagraphCount != paragraphList.Count)
        {
            message = $"指导布局的段落数量={guidingLayoutInfo.ParagraphCount} 与当前文本的段落数量={paragraphList.Count} 不匹配";
            return false;
        }

        for (int paragraphIndex = 0; paragraphIndex < paragraphList.Count; paragraphIndex++)
        {
            ParagraphData paragraphData = paragraphList[paragraphIndex];
            GuidingParagraphLayoutInfo guidingParagraph = guidingLayoutInfo.ParagraphList[paragraphIndex];
            if (guidingParagraph.ParagraphIndex != paragraphIndex)
            {
                message = $"指导布局的第 {paragraphIndex} 段索引值为 {guidingParagraph.ParagraphIndex.Index}，与实际段落序号不匹配";
                return false;
            }

            if (guidingParagraph.LineCount <= 0)
            {
                message = $"指导布局的第 {paragraphIndex} 段没有任何行";
                return false;
            }

            if (paragraphData.IsEmptyParagraph)
            {
                if (guidingParagraph.LineCount != 1 || guidingParagraph.CharCount != 0 || guidingParagraph.LineList[0].CharCount != 0)
                {
                    message = $"指导布局的第 {paragraphIndex} 段是空段，但其行信息与空段要求不匹配";
                    return false;
                }

                continue;
            }

            if (guidingParagraph.CharCount != paragraphData.CharCount)
            {
                message = $"指导布局的第 {paragraphIndex} 段字符数量={guidingParagraph.CharCount} 与当前段落字符数量={paragraphData.CharCount} 不匹配";
                return false;
            }

            int lineCharCountSum = guidingParagraph.LineList.Sum(t => t.CharCount);
            if (lineCharCountSum != paragraphData.CharCount)
            {
                message = $"指导布局的第 {paragraphIndex} 段各行字符总数={lineCharCountSum} 与当前段落字符数量={paragraphData.CharCount} 不匹配";
                return false;
            }

            for (int lineIndex = 0; lineIndex < guidingParagraph.LineList.Count; lineIndex++)
            {
                GuidingLineLayoutInfo guidingLine = guidingParagraph.LineList[lineIndex];
                if (guidingLine.ParagraphIndex != paragraphIndex)
                {
                    message = $"指导布局的第 {paragraphIndex} 段第 {lineIndex} 行段落索引为 {guidingLine.ParagraphIndex.Index}，与实际段落序号不匹配";
                    return false;
                }

                if (guidingLine.LineIndex != lineIndex)
                {
                    message = $"指导布局的第 {paragraphIndex} 段第 {lineIndex} 行索引值为 {guidingLine.LineIndex}，与实际行序号不匹配";
                    return false;
                }

                if (guidingLine.CharCount <= 0)
                {
                    message = $"指导布局的第 {paragraphIndex} 段第 {lineIndex} 行字符数量无效：{guidingLine.CharCount}";
                    return false;
                }
            }
        }

        message = string.Empty;
        return true;
    }
}

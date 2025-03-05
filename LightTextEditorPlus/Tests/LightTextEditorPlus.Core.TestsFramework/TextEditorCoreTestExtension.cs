using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Core.TestsFramework;

public static class TextEditorCoreTestExtension
{
    public static TextEditorCore UseFixedLineSpacing(this TextEditorCore textEditorCore, double fixedLineSpacing = 15)
    {
        textEditorCore.DocumentManager.SetStyleParagraphProperty(textEditorCore.DocumentManager.StyleParagraphProperty with
        {
            LineSpacing = TextLineSpacings.ExactlyLineSpace(fixedLineSpacing)
        });

        return textEditorCore;
    }

    /// <summary>
    /// 获取其布局换行的信息
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    public static string DumpBreakLineRenderInfo(this TextEditorCore textEditorCore)
    {
        StringBuilder stringBuilder = new StringBuilder();

        RenderInfoProvider renderInfoProvider = textEditorCore.GetRenderInfo();
        foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
        {
            foreach (ParagraphLineRenderInfo paragraphLineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
            {
                stringBuilder.Append(paragraphLineRenderInfo.Argument.CharList.ToText())
                    .Append("\n");
            }

            stringBuilder.Insert(stringBuilder.Length - 1, "\\n");
        }

        return stringBuilder.ToString();
    }
}

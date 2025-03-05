using System.Text;

using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 渲染信息的扩展方法
/// </summary>
public static class RenderInfoProviderExtension
{
    /// <summary>
    /// 获取其布局换行的信息。这一般是给调试用的信息，用于了解当前布局之后拿到的一行行信息
    /// </summary>
    /// <param name="renderInfoProvider"></param>
    /// <returns></returns>
    public static string DumpBreakLineRenderInfo(this RenderInfoProvider renderInfoProvider)
    {
        var stringBuilder = new StringBuilder();

        foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
        {
            foreach (ParagraphLineRenderInfo paragraphLineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
            {
                stringBuilder
                    .Append(paragraphLineRenderInfo.Argument.CharList.ToText())
                    .Append("\n");
            }

            stringBuilder.Insert(stringBuilder.Length - 1, "\\n");
        }

        return stringBuilder.ToString();
    }
}
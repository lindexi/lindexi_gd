using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils;

/// <summary>
/// 段落排版参数在布局过程的工具扩展
/// </summary>
internal static class ParagraphLayoutArgumentLayoutUtilsExtension
{
    /// <summary>
    /// 获取段前间距。首段不加段前间距
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    public static double GetParagraphBefore(this in ParagraphLayoutArgument argument)
    {
        ParagraphData paragraph = argument.ParagraphData;
        double paragraphBefore = argument.IsFirstParagraph ? 0 /*首段不加段前间距*/  : paragraph.ParagraphProperty.ParagraphBefore;
        return paragraphBefore;
    }

    /// <summary>
    /// 获取段后间距。最后一段不加段后间距
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    public static double GetParagraphAfter(this in ParagraphLayoutArgument argument)
    {
        ParagraphData paragraph = argument.ParagraphData;
        double paragraphAfter =
            argument.IsLastParagraph ? 0 /*最后一段不加段后间距*/ : paragraph.ParagraphProperty.ParagraphAfter;
        return paragraphAfter;
    }
}
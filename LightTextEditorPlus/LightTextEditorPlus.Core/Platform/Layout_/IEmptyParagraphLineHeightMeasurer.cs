using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 空段落的行高测量
/// </summary>
public interface IEmptyParagraphLineHeightMeasurer
{
    /// <summary>
    /// 对一个段落，这个段落没有任何内容，相当于段落在文本上只是一个换行符。测量此段落的高度
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    EmptyParagraphLineHeightMeasureResult MeasureEmptyParagraphLineHeight(in EmptyParagraphLineHeightMeasureArgument argument);
}
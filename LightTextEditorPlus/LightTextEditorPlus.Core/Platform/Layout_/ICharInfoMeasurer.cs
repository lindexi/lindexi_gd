using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 文本的字符测量器
/// </summary>
public interface ICharInfoMeasurer
{
    /// <summary>
    /// 测量字符信息
    /// </summary>
    /// <param name="charInfo"></param>
    /// <returns></returns>
    CharInfoMeasureResult MeasureCharInfo(in CharInfo charInfo);

    /// <summary>
    /// 测量字符信息。无论当前字符的尺寸是否测量过了，都会调入此方法。在此方法里面，可以超量测量，可以测量超过当前字符的其他字符的信息，可以一次性测量多个字符的信息
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    CharInfoMeasureResult MeasureCharInfo(in CharMeasureArgument argument);
}
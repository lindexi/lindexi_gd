using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 文本的字符测量器
/// </summary>
public interface ICharInfoMeasurer
{
    /// <summary>
    /// 测量和填充 Run 的尺寸信息。要求在此方法内必定完成对当前字符，即 argument.CurrentCharData 进行尺寸填充。可选进行超量测量，可以测量超过当前字符的其他字符的信息，可以一次性测量多个字符的信息
    /// </summary>
    /// 这是因为在一些平台里面，一口气测量一大段的文本字符的性能会明显优于一个个进行测量
    /// <param name="argument"></param>
    void MeasureAndFillSizeOfRun(in FillSizeOfRunArgument argument);
}
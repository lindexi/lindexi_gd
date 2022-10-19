using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 文本的字符测量器
/// </summary>
public interface ICharInfoMeasurer
{
    CharInfoMeasureResult MeasureCharInfo(in CharInfo charInfo);
}
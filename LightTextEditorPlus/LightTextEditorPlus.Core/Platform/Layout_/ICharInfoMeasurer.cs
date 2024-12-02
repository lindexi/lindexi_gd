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
}
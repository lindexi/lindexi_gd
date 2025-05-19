using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 文本的行内单个字符布局器，用于将单个字符布局到行内
/// </summary>
/// todo 改名为单个 Run 的布局器
public interface ISingleCharInLineLayouter
{
    /// <summary>
    /// 布局行内的单个字符
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    SingleCharInLineLayoutResult MeasureSingleRunLayout(in SingleCharInLineLayoutArgument argument);
}
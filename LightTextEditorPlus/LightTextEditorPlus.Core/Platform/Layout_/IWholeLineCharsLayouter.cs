using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 整行的字符布局器，用来布局一整行的字符，不包括行距等信息。只有字符排列
/// </summary>
public interface IWholeLineCharsLayouter
{
    /// <summary>
    /// 布局一行里面有哪些字符
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    WholeLineCharsLayoutResult UpdateWholeLineCharsLayout(in WholeLineLayoutArgument argument);
}
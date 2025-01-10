using System;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 字体回滚失败的事件参数
/// </summary>
public class FontFallbackFailedEventArgs : EventArgs
{
    /// <summary>
    /// 创建字体回滚失败的事件参数
    /// </summary>
    /// <param name="fontName"></param>
    public FontFallbackFailedEventArgs(string fontName)
    {
        FontName = fontName;
    }

    /// <summary>
    /// 回滚失败的字体名
    /// </summary>
    public string FontName { get; }
}

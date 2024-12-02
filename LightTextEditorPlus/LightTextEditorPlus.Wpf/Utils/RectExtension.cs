using System.Windows;

namespace LightTextEditorPlus.Utils;

/// <summary>
/// 对 Rect 的扩展
/// </summary>
public static class RectExtension
{
    /// <summary>
    /// 从文本通用的 Rect 转换为 WPF 的 Rect 类型
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static Rect ToWpfRect(this LightTextEditorPlus.Core.Primitive.TextRect rect) => new Rect(rect.X, rect.Y, rect.Width, rect.Height);
}
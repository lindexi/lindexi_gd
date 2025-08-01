using System.Windows;

namespace LightTextEditorPlus.Utils;

/// <summary>
/// 对 Point 的扩展
/// </summary>
public static class PointExtension
{
    /// <summary>
    /// 从文本的通用 Point 转换为 WPF 的 Point 类型
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Point ToWpfPoint(this LightTextEditorPlus.Core.Primitive.TextPoint point) => new Point(point.X, point.Y);

    /// <summary>
    /// 从 WPF 的 Point 转换为 文本的通用 Point 类型
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    internal static LightTextEditorPlus.Core.Primitive.TextPoint ToTextPoint(this Point point) => new LightTextEditorPlus.Core.Primitive.TextPoint(point.X, point.Y);
}

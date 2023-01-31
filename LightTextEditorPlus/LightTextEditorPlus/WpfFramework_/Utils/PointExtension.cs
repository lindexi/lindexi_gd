using System.Windows;

namespace LightTextEditorPlus.Utils;

public static class PointExtension
{
    public static Point ToWpfPoint(this LightTextEditorPlus.Core.Primitive.Point point) => new Point(point.X, point.Y);
}
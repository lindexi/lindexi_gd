using System.Windows;

namespace LightTextEditorPlus.Utils;

public static class PointExtension
{
    public static Point ToWpfPoint(this LightTextEditorPlus.Core.Primitive.Point point) => new Point(point.X, point.Y);

    internal static LightTextEditorPlus.Core.Primitive.Point ToPoint(this Point point) => new LightTextEditorPlus.Core.Primitive.Point(point.X, point.Y);
}
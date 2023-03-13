using System.Windows;

namespace LightTextEditorPlus.Utils;

public static class RectExtension
{
    public static Rect ToWpfRect(this LightTextEditorPlus.Core.Primitive.Rect rect) => new Rect(rect.X, rect.Y, rect.Width, rect.Height);
}
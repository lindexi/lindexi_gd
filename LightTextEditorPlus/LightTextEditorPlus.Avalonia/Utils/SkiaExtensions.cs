using Avalonia.Media;
using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus.Utils;

static class SkiaExtensions
{
    public static Color? ToAvaloniaColor(this SKColor? color)
    {
        return color == null ? null : ToAvaloniaColor(color.Value);
    }

    public static Color ToAvaloniaColor(this SKColor color)
    {
        return Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
    }

    public static SKColor? ToSKColor(this Color? color)
    {
        return color == null ? null : ToSKColor(color.Value);
    }

    public static SKColor ToSKColor(this Color color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }
}
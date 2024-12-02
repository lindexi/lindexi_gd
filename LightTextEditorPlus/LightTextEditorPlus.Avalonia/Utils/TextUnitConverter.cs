using Avalonia;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Utils;

static class TextUnitConverter
{
    public static TextPoint ToTextPoint(this Point point)
        => new(point.X, point.Y);

    public static Point ToPoint(this TextPoint textPoint)
        => new(textPoint.X, textPoint.Y);
}
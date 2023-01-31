using System.Windows;

namespace LightTextEditorPlus.Utils;

public static class SizeExtension
{
    public static Size ToWpfSize(this LightTextEditorPlus.Core.Primitive.Size size) => new Size(size.Width, size.Height);
}
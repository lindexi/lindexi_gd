using Avalonia.Media;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus;

public class CaretConfiguration : SkiaCaretConfiguration
{
    public new Color? CaretBrush
    {
        get => base.CaretBrush.ToAvaloniaColor();
        set => base.CaretBrush = value.ToSKColor();
    }

    public new Color SelectionBrush
    {
        get => base.SelectionBrush.ToAvaloniaColor();
        set => base.SelectionBrush = value.ToSKColor();
    }
}
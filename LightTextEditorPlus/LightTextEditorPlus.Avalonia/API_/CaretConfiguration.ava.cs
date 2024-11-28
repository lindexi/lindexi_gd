using Avalonia.Media;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus;

[APIConstraint("CaretConfiguration.txt", true)]
public class CaretConfiguration : SkiaCaretConfiguration
{
    public CaretConfiguration()
    {
    }

    internal CaretConfiguration(SkiaCaretConfiguration configuration)
    {
        CaretWidth = configuration.CaretWidth;
        base.CaretBrush = configuration.CaretBrush;
        base.SelectionBrush = configuration.SelectionBrush;
        CaretBlinkTime = configuration.CaretBlinkTime;
        ShowSelectionWhenNotInEditingInputMode = configuration.ShowSelectionWhenNotInEditingInputMode;
    }

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
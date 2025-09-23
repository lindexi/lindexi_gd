using Avalonia.Media;
using Avalonia.Skia;
using LightTextEditorPlus.Configurations;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Configurations;

/// <summary>
/// 光标的配置
/// </summary>
[APIConstraint("CaretConfiguration.txt", true)]
public class CaretConfiguration : SkiaCaretConfiguration
{
    /// <summary>
    /// 创建光标的配置
    /// </summary>
    public CaretConfiguration()
    {
    }

    internal CaretConfiguration(SkiaCaretConfiguration configuration)
    {
        CaretThickness = configuration.CaretThickness;
        base.CaretBrush = configuration.CaretBrush;
        base.SelectionBrush = configuration.SelectionBrush;
        CaretBlinkTime = configuration.CaretBlinkTime;
        ShowSelectionWhenNotInEditingInputMode = configuration.ShowSelectionWhenNotInEditingInputMode;
    }

    /// <summary>
    /// 光标的颜色
    /// </summary>
    public new Color? CaretBrush
    {
        get => base.CaretBrush.ToAvaloniaColor();
        set => base.CaretBrush = value?.ToSKColor();
    }

    /// <summary>
    /// 选择的颜色
    /// </summary>
    public new Color SelectionBrush
    {
        get => base.SelectionBrush.ToAvaloniaColor();
        set => base.SelectionBrush = value.ToSKColor();
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class TextEditorSettingsControl : UserControl
{
    public TextEditorSettingsControl()
    {
        InitializeComponent();
    }

    public TextEditor TextEditor { get; set; } = null!;

    private void FontNameComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        
    }

    private void ApplyFontSizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }

    private void ToggleBoldButton_OnClick(object? sender, RoutedEventArgs e)
    {
        
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace JowekukurNelaholefewhi;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ChangeTransparencyLevelHintButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TransparencyLevelHint = [WindowTransparencyLevel.None];
    }
}
using System.Linq;
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
        if (TransparencyLevelHint.First() == WindowTransparencyLevel.Transparent)
        {
            TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
        }
        else
        {
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        }
    }
}
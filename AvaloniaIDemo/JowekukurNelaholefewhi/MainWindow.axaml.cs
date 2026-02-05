using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Interactivity;

namespace JowekukurNelaholefewhi;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        //this.AttachDeveloperTools();
        this.AttachDevTools(new DevToolsOptions()
        {
            ShowAsChildWindow = true,
        });
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        
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
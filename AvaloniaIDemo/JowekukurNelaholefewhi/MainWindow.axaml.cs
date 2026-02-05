using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Interactivity;
using Avalonia.Rendering;

using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Skia;

namespace JowekukurNelaholefewhi;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        while (IsLoaded)
        {
            await Task.Delay(10);

            var color = new Color(0x02, NextByte(), NextByte(), NextByte());
            BackgroundBorder.Background = new ImmutableSolidColorBrush(color);
        }

        static byte NextByte() => (byte) Random.Shared.Next(byte.MaxValue);
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
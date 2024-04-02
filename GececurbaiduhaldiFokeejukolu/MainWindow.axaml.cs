using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

using BujeeberehemnaNurgacolarje;

using SkiaSharp;

namespace GececurbaiduhaldiFokeejukolu;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        TransparencyLevelHint = new[]
        {
            WindowTransparencyLevel.Transparent,
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.Blur,
        };
        TransparencyBackgroundFallback = Brushes.Transparent;
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsLinux())
        {
            if (TryGetPlatformHandle()?.Handle is { } handle)
            {
                // 此 handle 就是 X11 窗口的
                // 通过 xdotool set_window --name "Foo" {handle} 可以设置窗口标题
                Console.WriteLine(handle);

                var app = new BujeeberehemnaNurgacolarje.App();
                _app = app;
                Task.Run(() => app.Run(handle));

                var button = (Button) sender!;
                button.IsEnabled = false;
            }
        }
    }

    private BujeeberehemnaNurgacolarje.App? _app;

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    private void RedButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_app == null)
        {
            return;
        }

        _app.Color = SKColors.Red;
    }

    private void BlackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_app == null)
        {
            return;
        }
        _app.Color = SKColors.Black;
    }

    private void WhiteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_app == null)
        {
            return;
        }
        _app.Color = SKColors.White;
    }

    private void ClearButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _app.Clear();
    }
}
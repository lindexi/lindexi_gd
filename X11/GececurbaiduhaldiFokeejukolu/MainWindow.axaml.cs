using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // 方便调试
        StartInkMode();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_app is null)
        {
            StartInkMode();
        }
        else
        {
            _app.EnterPenMode();
        }
    }

    private void StartInkMode()
    {
        if (OperatingSystem.IsLinux())
        {
            if (TryGetPlatformHandle()?.Handle is { } handle)
            {
                // 此 handle 就是 X11 窗口的
                // 通过 xdotool set_window --name "Foo" {handle} 可以设置窗口标题
                Console.WriteLine(handle);

                var app = new BujeeberehemnaNurgacolarje.X11App();
                _app = app;
                Task.Run(() =>
                {
                    try
                    {
                        app.Run(handle);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
            }
        }
    }

    private BujeeberehemnaNurgacolarje.X11App? _app;

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
        if (_app == null)
        {
            return;
        }
        _app.Clear();
    }

    private void DebugModeToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var toggleButton = (ToggleButton) sender!;
        if (_app == null)
        {
            return;
        }

        _app.SwitchDebugMode(toggleButton.IsChecked is true);
    }

    private void DrawLineButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_app == null)
        {
            return;
        }

        _app.IsDrawLineMode = !_app.IsDrawLineMode;
    }

    private void EraserButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _app?.EnterEraserMode();
    }
}
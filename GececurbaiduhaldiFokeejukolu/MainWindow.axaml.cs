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
        // �������
        StartInkMode();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        StartInkMode();

        var button = (Button) sender!;
        button.IsEnabled = false;
    }

    private void StartInkMode()
    {
        if (OperatingSystem.IsLinux())
        {
            if (TryGetPlatformHandle()?.Handle is { } handle)
            {
                // �� handle ���� X11 ���ڵ�
                // ͨ�� xdotool set_window --name "Foo" {handle} �������ô��ڱ���
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
}
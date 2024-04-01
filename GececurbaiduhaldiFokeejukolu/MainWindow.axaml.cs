using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

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
            if (TryGetPlatformHandle()?.Handle is {} handle)
            {
                // �� handle ���� X11 ���ڵ�
                // ͨ�� xdotool set_window --name "Foo" {handle} �������ô��ڱ���
                Console.WriteLine(handle);

                var app = new BujeeberehemnaNurgacolarje.App();
                Task.Run(() => app.Run(handle));
            }
        }
    }
}
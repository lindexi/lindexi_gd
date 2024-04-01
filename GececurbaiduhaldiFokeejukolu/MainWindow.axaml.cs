using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GececurbaiduhaldiFokeejukolu;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsLinux())
        {
            if (TryGetPlatformHandle()?.Handle is {} handle)
            {
                // 此 handle 就是 X11 窗口的
                // 通过 xdotool set_window --name "Foo" {handle} 可以设置窗口标题
                Console.WriteLine(handle);

                var app = new BujeeberehemnaNurgacolarje.App();
                Task.Run(() => app.Run(handle));
            }
        }
    }
}
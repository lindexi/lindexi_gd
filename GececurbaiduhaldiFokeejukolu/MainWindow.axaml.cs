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
            var app = new BujeeberehemnaNurgacolarje.App();
            Task.Run(() => app.Run());
        }
    }
}
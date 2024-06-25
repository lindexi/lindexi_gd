using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace BallchairqeliralBileechenihawdanay;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        WindowState = WindowState.FullScreen;
        ShowInTaskbar = false;
    }
}
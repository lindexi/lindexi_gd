using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace JejanayaYemjergayle.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        this.Activated += MainWindow_Activated;
    }

    private void MainWindow_Activated(object? sender, EventArgs e)
    {
        var pointToScreen = this.PointToScreen(new Point(0, 0));
        Console.WriteLine($"MainWindow_Activated PointToScreen={pointToScreen}");
    }

    private async void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var pointToScreen = this.PointToScreen(new Point(0, 0));
        Console.WriteLine($"MainWindow_Loaded PointToScreen={pointToScreen}");

        await Task.Delay(1000);

        pointToScreen = this.PointToScreen(new Point(0, 0));
        Console.WriteLine(pointToScreen);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Controls;

namespace NarjejerechowainoBuwurjofear.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        for (int x = 0; x < 100; x++)
        {
            var n = Math.Pow(Math.E * x, Math.PI) * Math.Sin(Math.E * Math.PI * x) - Math.Pow(Math.E,Math.PI*x) * Math.Sin(Math.E * Math.PI * x);
            n = Math.Sin(Math.Pow(Math.E * x, Math.PI));
        }

        InitializeComponent();

        Loaded += MainView_Loaded;
    }

    private async void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        for (int i = 0; i < int.MaxValue; i++)
        {
            await Task.Delay(100);
            AvaSkiaInkCanvas.InvalidateVisual();
        }
    }
}
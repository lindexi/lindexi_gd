using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Controls;

namespace NarjejerechowainoBuwurjofear.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        Loaded += MainView_Loaded;

        RootGrid.PointerPressed += RootGrid_PointerPressed;
    }

    private void RootGrid_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(RootGrid);
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
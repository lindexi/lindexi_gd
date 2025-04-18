using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FebairwemliwoNajojali;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }
}

public class AvaSkiaInkCanvas : Control
{

}
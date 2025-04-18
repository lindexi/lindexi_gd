using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

using InkBase;

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

    private void ReduceInkThicknessButton_OnClick(object? sender, RoutedEventArgs e)
    {
        InkingAcceleratorLayer.InkThickness--;
    }

    private void AddInkThicknessButton_OnClick(object? sender, RoutedEventArgs e)
    {
        InkingAcceleratorLayer.InkThickness++;
    }

    private void ToggleStrokeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        InkingAcceleratorLayer.ToggleShowHideAllStroke();
    }

    private IWpfInkLayer InkingAcceleratorLayer => WpfForAvaloniaInkingAccelerator.Instance.InkLayer;
}
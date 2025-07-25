using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using System;
using System.IO;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Threading;

namespace CibairfejeballChecekayral.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        Loaded += MainView_Loaded;
    }

    private void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        var solidColorBrush = new SolidColorBrush(Colors.Red, 0.5);
        IImmutableSolidColorBrush immutableSolidColorBrush = new ImmutableSolidColorBrush(solidColorBrush);

        var color = immutableSolidColorBrush.Color;

        Background = immutableSolidColorBrush;
    }
}
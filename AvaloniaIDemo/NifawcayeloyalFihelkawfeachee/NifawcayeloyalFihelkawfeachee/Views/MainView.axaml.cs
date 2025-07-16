using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using System;
using System.IO;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace NifawcayeloyalFihelkawfeachee.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void Button_OnLoaded(object? sender, RoutedEventArgs e)
    {
        var animation = (Animation) RootGrid.Resources["FooAnimation"]!;
        _ = animation!.RunAsync((Button) sender);
    }
}
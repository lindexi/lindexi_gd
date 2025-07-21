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
using SkiaSharp;
using Svg.Skia;

namespace HawhihabaiwhaNealaykafo.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        Loaded += MainView_Loaded;
    }

    private void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        var file = Path.Join(AppContext.BaseDirectory, "Assets", "file0000.svg");
        
        var skSvg = new SKSvg();
        var skPicture = skSvg.Load(file);
        var outputFile = Path.GetFullPath("1.png");
        var canSave = skSvg.Save(outputFile, SKColor.Empty);
    }
}
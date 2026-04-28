using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Path = System.IO.Path;

namespace WulaycehaRerwurlarrurburkerejea;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        using var renderTargetBitmap = new RenderTargetBitmap(new PixelSize(1920, 1080));
        renderTargetBitmap.Render(this);

        var imageFile = Path.GetFullPath($"{Path.GetRandomFileName()}.png");
        renderTargetBitmap.Save(imageFile);
        Console.WriteLine($"ImageFile={imageFile}， WH={Bounds.Width:0.00}x{Bounds.Height:0.00}");
    }
}
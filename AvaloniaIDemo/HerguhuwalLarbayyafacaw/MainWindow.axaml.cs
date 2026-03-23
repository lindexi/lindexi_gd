using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

using System.Diagnostics;
using System.IO;

namespace HerguhuwalLarbayyafacaw;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var control = Border1;

        var renderTargetBitmap = new RenderTargetBitmap(new PixelSize((int) control.DesiredSize.Width, (int) control.DesiredSize.Height));
        renderTargetBitmap.Render(control);
        var file = Path.GetFullPath("1.png");
        renderTargetBitmap.Save(file);
        Process.Start("explorer.exe", file);
    }
}
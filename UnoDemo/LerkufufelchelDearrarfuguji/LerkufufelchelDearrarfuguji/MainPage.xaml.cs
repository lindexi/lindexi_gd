using SkiaSharp.Views.Windows;

namespace LerkufufelchelDearrarfuguji;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        SkXamlCanvas.Invalidate();
    }

    private void SkXamlCanvas_OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Raise PaintSurface");
    }
}

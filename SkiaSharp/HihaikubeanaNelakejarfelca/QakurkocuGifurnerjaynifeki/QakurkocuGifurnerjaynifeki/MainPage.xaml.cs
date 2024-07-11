using SkiaSharp.Views.Windows;
using WhaleljainaDeljayfecelchalearqe;

namespace QakurkocuGifurnerjaynifeki;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        
    }

    private void SkXamlCanvas_OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var skCanvasTest = new SKCanvasTest();
        skCanvasTest.Test(e.Surface.Canvas);
    }
}

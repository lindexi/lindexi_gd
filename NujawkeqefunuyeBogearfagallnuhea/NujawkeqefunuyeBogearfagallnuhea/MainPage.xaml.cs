
using System.ComponentModel;
using System.Diagnostics;
using Windows.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using NujawkeqefunuyeBogearfagallnuhea.UnoSpySnoop;
using SkiaSharp;

namespace NujawkeqefunuyeBogearfagallnuhea;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        UnoSpySnoop.SpySnoop.StartSpyUI(RootGrid);

        var bitmapImage = new BitmapImage();
        var file = Path.GetFullPath("Image.jpg");
        var skImageInfo = SKBitmap.DecodeBounds(file);

        bitmapImage.ImageOpened += async (sender, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"DecodePixelWidth={bitmapImage.DecodePixelWidth} DecodePixelHeight={bitmapImage.DecodePixelHeight} PixelWidth={bitmapImage.PixelWidth} PixelHeight={bitmapImage.PixelHeight}");

            // Hack: After delay 500ms, I can get the pixel size
            await Task.Delay(500);

            System.Diagnostics.Debug.WriteLine($"DecodePixelWidth={bitmapImage.DecodePixelWidth} DecodePixelHeight={bitmapImage.DecodePixelHeight} PixelWidth={bitmapImage.PixelWidth} PixelHeight={bitmapImage.PixelHeight}");
        };
        bitmapImage.UriSource = new Uri(file);

        var image = new Image()
        {
            Source = bitmapImage,
        };
        image.Loaded += (sender, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"ImageLoaded Width={bitmapImage.DecodePixelWidth} Height={bitmapImage.DecodePixelHeight}");
        };

        var border = new Border()
        {
            Width = 100,
            Height = 100,
            Child = image,
        };

        RootPanel.Children.Add(border);
    }
}

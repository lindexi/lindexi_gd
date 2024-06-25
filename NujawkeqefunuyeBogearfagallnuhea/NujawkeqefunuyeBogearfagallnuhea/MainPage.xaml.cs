using System.Diagnostics;

using Microsoft.UI.Xaml.Media.Imaging;

namespace NujawkeqefunuyeBogearfagallnuhea;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        var bitmapImage = new BitmapImage();
        var file = Path.GetFullPath("Image.jpg");
        bitmapImage.ImageOpened += (sender, args) =>
        {
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

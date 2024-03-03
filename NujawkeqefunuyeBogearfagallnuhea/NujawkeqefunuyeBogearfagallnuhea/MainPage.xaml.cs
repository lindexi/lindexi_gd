
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
        Debug.Assert(File.Exists(file));
        bitmapImage.ImageOpened += (sender, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"Width={bitmapImage.DecodePixelWidth} Height={bitmapImage.DecodePixelHeight}");
        };
        bitmapImage.UriSource = new Uri(file);

        var image = new Image()
        {
            Source = bitmapImage
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

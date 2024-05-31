using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;

namespace UnoInk;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }
    
    private async void ScreenshotButton_OnClick(object sender, RoutedEventArgs e)
    {
        var renderTargetBitmap = new RenderTargetBitmap();
        await renderTargetBitmap.RenderAsync(this);
        
        var buffer = await renderTargetBitmap.GetPixelsAsync();
        var file = "1.png";
        
        using var fileStream = File.OpenWrite(file);
        IRandomAccessStream stream = fileStream.AsRandomAccessStream();

        var bitmapEncoder = await BitmapEncoder.CreateAsync(
                BitmapEncoder.JpegEncoderId, stream);
        bitmapEncoder.SetPixelData(BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            (uint) renderTargetBitmap.PixelWidth,
            (uint) renderTargetBitmap.PixelHeight,
            DisplayInformation.GetForCurrentView().LogicalDpi,
            DisplayInformation.GetForCurrentView().LogicalDpi,
            buffer.ToArray()
        );
        await bitmapEncoder.FlushAsync();
    }
}

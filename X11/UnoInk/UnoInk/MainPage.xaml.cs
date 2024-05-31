<<<<<<< HEAD
<<<<<<< HEAD
=======
=======
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;

<<<<<<< HEAD
>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
=======
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b
namespace UnoInk;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
<<<<<<< HEAD
<<<<<<< HEAD

=======
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
>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
=======
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b
    }
}

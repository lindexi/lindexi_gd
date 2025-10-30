using Microsoft.UI.Xaml.Media.Imaging;
using Uno.Disposables;

namespace UnoInk.Presentation;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }
    
    private async void SnapButton_OnClick(object sender, RoutedEventArgs e)
    {
        var renderTargetBitmap = new RenderTargetBitmap();
        renderTargetBitmap.RenderAsync(this);
        var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        // +		pixelBuffer	{Windows.Storage.Streams.Buffer}	Windows.Storage.Streams.IBuffer {Windows.Storage.Streams.Buffer}
        if (pixelBuffer is Windows.Storage.Streams.Buffer buffer)
        {
        }
        renderTargetBitmap.TryDispose();
    }
}

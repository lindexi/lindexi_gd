using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using System;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;

namespace GelwhalhahonelGilerewalfee.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += MainView_Loaded;
    }

    private void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (this.TryGetResource("Image.Startup", out var source) && source is DrawingImage drawingImage)
        {
            var imageSize = drawingImage.Size;
            using var renderTargetBitmap = new RenderTargetBitmap(new PixelSize((int) imageSize.Width, (int) imageSize.Height));

            using (var drawingContext = renderTargetBitmap.CreateDrawingContext())
            {
                drawingContext.DrawImage(drawingImage, new Rect(new Point(), imageSize));
            }
           
            renderTargetBitmap.Save("1.png");
        }
    }
}
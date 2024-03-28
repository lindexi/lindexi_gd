using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

var thread = new Thread(() =>
{
    var application = new Application();
    application.Startup += Application_Startup;
    application.Run();
});

void Application_Startup(object sender, StartupEventArgs e)
{
    var drawingVisual = new DrawingVisual();
    using (var drawingContext = drawingVisual.RenderOpen())
    {
        drawingContext.DrawRectangle(Brushes.Black, pen: null, new Rect(0, 0, 1024, 768));
    }

    // 画布大小
    var drawingBounds = drawingVisual.Drawing.Bounds;
    // 修改为固定的尺寸
    drawingBounds = new Rect(0, 0, 1024, 768);
    var renderTargetBitmap = new RenderTargetBitmap((int) drawingBounds.Width, (int) drawingBounds.Height, 96, 96, PixelFormats.Pbgra32);
    renderTargetBitmap.Render(drawingVisual);

    var pngBitmapEncoder = new PngBitmapEncoder();
    pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

    var file = "1.png";

    using (var fileStream = File.Create(file))
    {
        pngBitmapEncoder.Save(fileStream);
    }
}

thread.SetApartmentState(ApartmentState.STA);
thread.Start();
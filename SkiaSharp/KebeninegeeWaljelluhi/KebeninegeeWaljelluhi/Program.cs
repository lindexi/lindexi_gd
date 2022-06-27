// See https://aka.ms/new-console-template for more information

using SkiaSharp;

var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());

using (var surface = SKSurface.Create(skImageInfo))
{
    // the the canvas and properties
    var canvas = surface.Canvas;

    // make sure the canvas is blank
    canvas.Clear(SKColors.White);

    // draw some text
    var paint = new SKPaint
    {
        Color = SKColors.Black,
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        TextAlign = SKTextAlign.Center,
        TextSize = 24
    };
    var coord = new SKPoint(skImageInfo.Width / 2, (skImageInfo.Height + paint.TextSize) / 2);
    canvas.DrawText("SkiaSharp", coord, paint);

    // save the file
    using (var image = surface.Snapshot())
    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
    using (var stream = File.OpenWrite("output.png"))
    {
        data.SaveTo(stream);
    }
}

using var skImage = SKImage.Create(skImageInfo);

using (SKBitmap skBitmap = SKBitmap.FromImage(skImage))
{
    using (var skCanvas = new SKCanvas(skBitmap))
    {
        skCanvas.Clear(new SKColor(130, 130, 130));
        skCanvas.DrawText("SkiaSharp Console!", 50, 200, new SKPaint() { Color = new SKColor(0, 0, 0), TextSize = 100 });
        skCanvas.Flush();

        using (var skData = skBitmap.Encode(SKEncodedImageFormat.Png, 100))
        {
            var file = new FileInfo("Test.png");
            using (var fileStream = file.OpenWrite())
            {
                fileStream.SetLength(0);
                skData.SaveTo(fileStream);
            }
        }
    }
}
// See https://aka.ms/new-console-template for more information

using SkiaSharp;

var width = 1920;
var height = 1080;

var originBitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888));
{
    using (var skCanvas = new SKCanvas(originBitmap))
    {
        skCanvas.Clear(SKColors.White);
        skCanvas.DrawRect(100, 100, 200, 100, new SKPaint()
        {
            Color = SKColors.Red,
            Style = SKPaintStyle.Fill,
        });
    }
}

SKRect rect = new SKRect(0, 0, width, height);
var skMatrix = SKMatrix.CreateRotation(MathF.PI / 2, width / 2f, height / 2f);
//.PreConcat(SKMatrix.CreateTranslation(-width / 2f, -height / 2f))
//.PostConcat(SKMatrix.CreateTranslation(height / 2f, width / 2f));

rect = skMatrix.MapRect(rect);

using var skBitmap = new SKBitmap(new SKImageInfo((int) rect.Width, (int) rect.Height, SKColorType.Bgra8888));
using (var skCanvas = new SKCanvas(skBitmap))
{
    skCanvas.Clear(SKColors.Blue);
    skCanvas.SetMatrix(skMatrix);
    skCanvas.DrawBitmap(originBitmap, 0, 0);
}

var file = "1.png";

var skData = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
using var fileStream = File.OpenWrite(file);
skData.SaveTo(fileStream);

Console.WriteLine("Hello, World!");

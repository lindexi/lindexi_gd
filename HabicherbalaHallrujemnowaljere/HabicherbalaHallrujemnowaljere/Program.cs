// See https://aka.ms/new-console-template for more information

using SkiaSharp;

var fileName = $"output.png";

var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Opaque, SKColorSpace.CreateSrgb());

using var skImage = SKImage.Create(skImageInfo);

using (SKBitmap skBitmap = SKBitmap.FromImage(skImage))
{
    using (var skCanvas = new SKCanvas(skBitmap))
    {
        skCanvas.Clear(SKColors.White);

        var inputFile = new FileInfo("Test.png");

        using var fileStream1 = inputFile.OpenRead();

        using var resourceBitmap1 = SKBitmap.Decode(fileStream1);

        // 这个解码会变糊
        skCanvas.DrawBitmap(resourceBitmap1, new SKPoint(0, 0));

        skCanvas.Flush();

        using (var skData = skBitmap.Encode(SKEncodedImageFormat.Png, 100))
        {
            var file = new FileInfo(fileName);
            using (var fileStream = file.OpenWrite())
            {
                fileStream.SetLength(0);
                skData.SaveTo(fileStream);
            }
        }
    }
}
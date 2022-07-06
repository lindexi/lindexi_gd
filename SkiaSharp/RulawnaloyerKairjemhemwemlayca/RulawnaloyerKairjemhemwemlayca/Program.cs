using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

using SkiaSharp;

var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Unpremul, SKColorSpace.CreateSrgb());

var fileName = $"xx.png";

using (var skImage = SKImage.Create(skImageInfo))
{
    using (var skBitmap = SKBitmap.FromImage(skImage))
    {
        using (var skCanvas = new SKCanvas(skBitmap))
        {
            skCanvas.Clear(SKColors.Transparent);

            var skiaCanvas = new SkiaCanvas();
            skiaCanvas.Canvas = skCanvas;

            ICanvas canvas = skiaCanvas;
            canvas.Font = new Font("微软雅黑");

            canvas.FontSize = 100;
            canvas.DrawString("汉字", 100, 100, 500, 500, HorizontalAlignment.Left, VerticalAlignment.Top);
            canvas.StrokeColor = Colors.Blue;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(100, 100, 500, 500);

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
}

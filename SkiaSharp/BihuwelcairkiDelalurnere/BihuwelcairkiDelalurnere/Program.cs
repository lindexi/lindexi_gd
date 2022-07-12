// See https://aka.ms/new-console-template for more information

using BihuwelcairkiDelalurnere;

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
            skCanvas.Clear(SKColors.White);

            var skiaCanvas = new SkiaCanvas();
            skiaCanvas.Canvas = skCanvas;

            ICanvas canvas = skiaCanvas;
            var areaDraw = new AreaDraw();
            areaDraw.OnRender(canvas);
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

System.Console.WriteLine("生成图表完成，输出图片 xx.png ");
System.Console.Read();
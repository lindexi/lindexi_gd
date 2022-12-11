// See https://aka.ms/new-console-template for more information

<<<<<<< HEAD
using BihuwelcairkiDelalurnere;

using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

=======
>>>>>>> 0dc53049b7b74e57472408db161d2c066755bace
using SkiaSharp;

var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Unpremul, SKColorSpace.CreateSrgb());

var fileName = $"xx.png";

using (var skImage = SKImage.Create(skImageInfo))
{
    using (var skBitmap = SKBitmap.FromImage(skImage))
    {
<<<<<<< HEAD
        using (var skCanvas = new SKCanvas(skBitmap))
=======
        skCanvas.Clear(SKColors.White);

        var name = "微软雅黑";
        var skTypeface = SKTypeface.FromFamilyName(name);

        var skPaint = new SKPaint(skTypeface.ToFont(30))
        {
            Color = SKColors.Black,
        };

        var skTextBlob = SKTextBlob.Create("林德熙", skTypeface.ToFont(30), new SKPoint(10, 30));
        skCanvas.DrawText(skTextBlob, 10, 10, skPaint);

        skCanvas.Flush();

        using (var skData = skBitmap.Encode(SKEncodedImageFormat.Png, 100))
>>>>>>> 0dc53049b7b74e57472408db161d2c066755bace
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
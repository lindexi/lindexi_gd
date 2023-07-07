// See https://aka.ms/new-console-template for more information

<<<<<<< HEAD
using BihuwelcairkiDelalurnere;

using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

<<<<<<< HEAD
=======
>>>>>>> 0dc53049b7b74e57472408db161d2c066755bace
using SkiaSharp;

var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Unpremul, SKColorSpace.CreateSrgb());
=======
using SkiaSharp;

var width = 1920;
var height = 1080;
var skImageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque, SKColorSpace.CreateSrgb());
>>>>>>> 407cf155afdb5e60227ff915ddcd85928ed8dccd

var fileName = $"xx.png";

using (var skImage = SKImage.Create(skImageInfo))
{
    using (var skBitmap = SKBitmap.FromImage(skImage))
    {
<<<<<<< HEAD
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
=======
        skCanvas.Clear(SKColor.Parse("FFF"));
        

        foreach (var defaultFontFamily in SkiaSharp.SKFontManager.Default.FontFamilies)
        {
            Console.WriteLine(defaultFontFamily);
        }

        var skTypeface = SkiaSharp.SKFontManager.Default.CreateTypeface("/usr/share/fonts/simkai.ttf");
        var skFont = new SKFont(skTypeface, 100);
        skCanvas.DrawText("汉字", 100, 100, new SKPaint(skFont)
        {
            // 反锯齿
            IsAntialias = true
        });

        //skCanvas.DrawText();

        //var skiaCanvas = new SkiaCanvas();
        //skiaCanvas.Canvas = skCanvas;

        //ICanvas canvas = skiaCanvas;

        //canvas.StrokeSize = 2;
        //canvas.StrokeColor = Colors.Blue;

        //canvas.DrawLine(10, 10, 100, 10);

        //canvas.Font = new Font("KaiTi");

        //canvas.FontSize = 100;
        //canvas.DrawString("汉字", 100, 100, HorizontalAlignment.Left);
        //canvas.StrokeColor = Colors.Blue;
        //canvas.StrokeSize = 2;
        //canvas.DrawRectangle(100, 100, 500, 500);
>>>>>>> 407cf155afdb5e60227ff915ddcd85928ed8dccd

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

<<<<<<< HEAD
System.Console.WriteLine("生成图表完成，输出图片 xx.png ");
System.Console.Read();
=======
class MauiSkiaCanvas : SkiaCanvas
{

}
>>>>>>> 407cf155afdb5e60227ff915ddcd85928ed8dccd

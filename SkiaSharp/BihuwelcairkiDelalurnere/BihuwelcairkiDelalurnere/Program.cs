// See https://aka.ms/new-console-template for more information

using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

using SkiaSharp;

var width = 1920;
var height = 1080;
var skImageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque, SKColorSpace.CreateSrgb());

var fileName = $"xx.png";

using var skImage = SKImage.Create(skImageInfo);
using (SKBitmap skBitmap = SKBitmap.FromImage(skImage))
{
    using (var skCanvas = new SKCanvas(skBitmap))
    {
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

class MauiSkiaCanvas : SkiaCanvas
{

}
// See https://aka.ms/new-console-template for more information

using SkiaSharp;

using var skTypeface =
    SKFontManager.Default.MatchFamily("微软雅黑");

using var skBitmap = new SKBitmap(300, 300, SKColorType.Bgra8888, SKAlphaType.Premul);
skBitmap.Erase(SKColors.White);
using var skCanvas = new SKCanvas(skBitmap);

var skFont = skTypeface.ToFont(50);

using var skPaint = new SKPaint();
skPaint.Color = SKColors.Black;
skPaint.IsAntialias = true;

var skTextBlob = SKTextBlob.Create([0x00, 0x00], SKTextEncoding.GlyphId, skFont, new SKPoint(50, 100));

skCanvas.DrawText(skTextBlob, 50, 100, skPaint);

var outputFile = Path.Join(AppContext.BaseDirectory, $"1.png");

using (var outputStream = File.OpenWrite(outputFile))
{
    skBitmap.Encode(outputStream, SKEncodedImageFormat.Png, 100);
}

Console.Read();
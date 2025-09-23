// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using SkiaSharp;

foreach (var fontName in SKFontManager.Default.FontFamilies)
{
    using var skTypeface = SKFontManager.Default.MatchFamily(fontName);
    if (skTypeface is null)
    {
        continue;
    }

    using var skBitmap = new SKBitmap(300, 300, SKColorType.Bgra8888, SKAlphaType.Premul);
    skBitmap.Erase(SKColors.White);
    using var skCanvas = new SKCanvas(skBitmap);

    var skFont = skTypeface.ToFont(50);

    using var skPaint = new SKPaint();
    skPaint.Color = SKColors.Black;
    skPaint.IsAntialias = true;

    Span<ushort> glyphs = [0x00, 0x00];

    var skTextBlob = SKTextBlob.Create(MemoryMarshal.AsBytes(glyphs), SKTextEncoding.GlyphId, skFont);

    skCanvas.DrawText(skTextBlob, 50, 100, skPaint);

    var outputFile = Path.Join(AppContext.BaseDirectory, $"{fontName}.png");

    using (var outputStream = File.OpenWrite(outputFile))
    {
        skBitmap.Encode(outputStream, SKEncodedImageFormat.Png, 100);
    }
}

Console.Read();
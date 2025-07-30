// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using SkiaSharp;

var symbolFontFile = Path.Join(AppContext.BaseDirectory, "StandardSymbolsPS.ttf");

using var skTypeface =
    SKFontManager.Default.CreateTypeface(symbolFontFile);
Console.WriteLine($"Font='{symbolFontFile}' SKTypeface={skTypeface.FamilyName} GlyphCount={skTypeface.GlyphCount}");

var text = "p"; // 这里的 p 是 Symbol 字体中的 Pi 符号
char testChar = text[0];

Console.WriteLine($"ContainsGlyph('{testChar}')={skTypeface.ContainsGlyph(testChar)} {skTypeface.GetGlyph(testChar)}");

using var skBitmap = new SKBitmap(300, 300, SKColorType.Bgra8888, SKAlphaType.Premul);
skBitmap.Erase(SKColors.White);
using var skCanvas = new SKCanvas(skBitmap);

var skFont = skTypeface.ToFont(50);

using var skPaint = new SKPaint();
skPaint.Color = SKColors.Black;
skPaint.IsAntialias = true;

skCanvas.DrawText(text, 50, 100, skFont, skPaint);

var outputFile = Path.Join(AppContext.BaseDirectory, $"1.png");

using (var outputStream = File.OpenWrite(outputFile))
{
    skBitmap.Encode(outputStream, SKEncodedImageFormat.Png, 100);
}

Console.Read();

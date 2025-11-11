// See https://aka.ms/new-console-template for more information

using SkiaSharp;

using var skTypeface =
    SKFontManager.Default.MatchFamily("HarmonyOS Sans SC");
Console.WriteLine($"SKTypeface={skTypeface.FamilyName} GlyphCount={skTypeface.GlyphCount}");

var text = "adj";
char testChar = text[0];

Console.WriteLine($"ContainsGlyph('{testChar}')={skTypeface.ContainsGlyph(testChar)} {skTypeface.GetGlyph(testChar)}");

using var skBitmap = new SKBitmap(300, 300, SKColorType.Bgra8888, SKAlphaType.Premul);
skBitmap.Erase(SKColors.White);
using var skCanvas = new SKCanvas(skBitmap);

var skFont = skTypeface.ToFont(48);

using var skPaint = new SKPaint();
skPaint.Color = SKColors.Black;
skPaint.IsAntialias = true;
skPaint.Style = SKPaintStyle.Stroke;

skCanvas.DrawRect(10, 10, 270, 270, skPaint);

// The main reproducing setting:
var edging = SKFontEdging.SubpixelAntialias;
skFont.Edging = edging;
skFont.Hinting = SKFontHinting.Full;
skFont.Edging = edging;
skFont.Subpixel = edging != SKFontEdging.SubpixelAntialias;

skPaint.Style = SKPaintStyle.Fill;

skCanvas.DrawText(text, 50, 100, skFont, skPaint);

var outputFile = Path.Join(AppContext.BaseDirectory, $"1.png");

using (var outputStream = File.OpenWrite(outputFile))
{
    skBitmap.Encode(outputStream, SKEncodedImageFormat.Png, 100);
}

Console.Read();

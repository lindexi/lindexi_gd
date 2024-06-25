// See https://aka.ms/new-console-template for more information

using SkiaSharp;

var skTypeFace = SKTypeface.FromFamilyName(null, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
Console.WriteLine($"fromFamilyName={skTypeFace} FamilyName={skTypeFace?.FamilyName} GlyphCount={skTypeFace?.GlyphCount}");

var fontSize = 20;
var skFont = new SKFont(skTypeFace, fontSize);
skFont.Edging = SKFontEdging.SubpixelAntialias;
skFont.Subpixel = true;



Console.WriteLine($"SKFont={skFont.Size}, {skFont.ScaleX}, {skFont.Metrics}");
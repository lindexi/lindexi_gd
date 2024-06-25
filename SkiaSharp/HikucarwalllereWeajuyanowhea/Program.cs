// See https://aka.ms/new-console-template for more information

using SkiaSharp;

var skTypeFace = SKTypeface.FromFamilyName(null, SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);

if (skTypeFace is null)
{
    Console.WriteLine($"skTypeFace is null");
}

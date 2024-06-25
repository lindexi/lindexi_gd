// See https://aka.ms/new-console-template for more information

using SkiaSharp;

var fromFamilyName = SKTypeface.FromFamilyName(null, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
Console.WriteLine($"fromFamilyName={fromFamilyName}");

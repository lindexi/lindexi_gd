// See https://aka.ms/new-console-template for more information
using SkiaSharp;

using System.Globalization;
using System.Text.RegularExpressions;

var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Opaque, SKColorSpace.CreateSrgb());
using var skBitmap = new SKBitmap(skImageInfo);
using var skCanvas = new SKCanvas(skBitmap);

SKFontManager skFontManager = SKFontManager.Default;

var languageTagBuffer = new string[2];
languageTagBuffer[0] = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
languageTagBuffer[1] = CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName;

for (int i = 0; i < 100000; i++)
{
    var skTypeface = skFontManager.MatchCharacter(null, SKFontStyle.Normal, languageTagBuffer, '1');
    if (skTypeface != null)
    {
        i--;
    }
}

Console.WriteLine("Hello, World!");

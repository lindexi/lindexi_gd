// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using SkiaSharp;

var fontFamilies = SKFontManager.Default.GetFontFamilies();

var notFoundNameList = new List<string>();

for (int i = 0; i < 100; i++)
{
    notFoundNameList.Add($"FontNameNotFound{i}");
}

var stopwatch = Stopwatch.StartNew();
for (var i = 0; i < 100; i++)
{
    var typeface = SKTypeface.FromFamilyName("Î¢ÈíÑÅºÚ");
    _ = typeface;
    foreach (var fontFamily in fontFamilies)
    {
        _ = SKTypeface.FromFamilyName(fontFamily);
    }

    foreach (var fontName in notFoundNameList)
    {
        _ = SKTypeface.FromFamilyName(fontName);
    }
}

stopwatch.Stop();
Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms Ave:{stopwatch.Elapsed.TotalMilliseconds / (fontFamilies.Length + notFoundNameList.Count) / 100}ms FontFamilies={fontFamilies.Length}");

Console.Read();
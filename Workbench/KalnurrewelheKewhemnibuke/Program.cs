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
    foreach (var fontName in notFoundNameList)
    {
        var typeface = SKFontManager.Default.MatchFamily(fontName);
        _ = typeface;
    }
}

stopwatch.Stop();
Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms Ave:{stopwatch.Elapsed.TotalMilliseconds / notFoundNameList.Count / 100}ms FontFamilies={notFoundNameList.Count}");

Console.Read();
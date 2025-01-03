// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using SkiaSharp;

var fontFamilies = SKFontManager.Default.GetFontFamilies();

var notFoundNameList = new List<string>();

for (int i = 0; i < 100; i++)
{
    notFoundNameList.Add($"FontNameNotFound{i}");
}

foreach (var fontName in notFoundNameList)
{
    var typeface = SKTypeface.FromFamilyName(fontName);
}

Console.Read();
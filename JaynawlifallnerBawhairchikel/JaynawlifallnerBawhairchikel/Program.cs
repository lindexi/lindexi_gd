using System.Runtime.InteropServices;
using SkiaSharp;

var skImageInfo = new SKImageInfo(1000, 1000, SKColorType.Bgra8888, SKAlphaType.Premul);

using var skSurface = SKSurface.Create(skImageInfo);

var canvas = skSurface.Canvas;



// make sure the canvas is blank
canvas.Clear(SKColors.White);

var fontName = SKFontManager.Default.FontFamilies.FirstOrDefault(t => t == "宋体");

var typeface =
    sk_fontmgr_match_family_style(SKFontManager.Default.Handle, "宋体", SKFontStyle.Normal.Handle); // OK

var typeface2 = SKFontManager.Default.MatchFamily("宋体"); // Fail

Console.Read();

[DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
static extern IntPtr sk_fontmgr_match_family_style(
    IntPtr param0,
    [MarshalAs(UnmanagedType.LPUTF8Str)] string familyName,
    IntPtr style);


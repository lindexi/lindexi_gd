using System.Runtime.InteropServices;
using System.Text;
using SkiaSharp;
using BindingFlags = System.Reflection.BindingFlags;

var skImageInfo = new SKImageInfo(1000, 1000, SKColorType.Bgra8888, SKAlphaType.Premul);

using var skSurface = SKSurface.Create(skImageInfo);

var canvas = skSurface.Canvas;



// make sure the canvas is blank
canvas.Clear(SKColors.White);

var fontName = SKFontManager.Default.FontFamilies.FirstOrDefault(t => t == "宋体");

var utf8ByteList = Encoding.UTF8.GetBytes("宋体");

IntPtr typefaceHandle;
unsafe
{
    fixed (byte* p = utf8ByteList)
    {
        typefaceHandle =
            sk_fontmgr_match_family_style(SKFontManager.Default.Handle, new IntPtr(p),SKFontStyle.Normal.Handle); // OK
    }
}

var typeface2 = SKFontManager.Default.MatchFamily("宋体"); // Fail

[DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
static extern IntPtr sk_fontmgr_match_family_style(
    IntPtr param0,
    IntPtr familyName,
    IntPtr style);

var constructorInfo = typeof(SKTypeface).GetConstructor(BindingFlags.NonPublic| BindingFlags.Instance,new []{typeof(IntPtr),typeof(bool)});
SKTypeface typeface = (SKTypeface)constructorInfo!.Invoke(new object[] { typefaceHandle, true });

// draw some text
var paint = new SKPaint
{
    Color = SKColors.Black,
    IsAntialias = true,
    Style = SKPaintStyle.Fill,
    TextAlign = SKTextAlign.Center,
    TextSize = 30,
    Typeface = typeface,
};
var coord = new SKPoint(skImageInfo.Width / 2, (skImageInfo.Height + paint.TextSize) / 2);
canvas.DrawText("林德熙是逗比", coord, paint);

// save the file
using (var image = skSurface.Snapshot())
using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
using (var stream = File.OpenWrite("output.png"))
{
    data.SaveTo(stream);
}


Console.Read();

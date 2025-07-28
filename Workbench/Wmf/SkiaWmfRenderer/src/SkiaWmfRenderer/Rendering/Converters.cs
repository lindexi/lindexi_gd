using Oxage.Wmf;

using SkiaSharp;

using System.Text;
using Oxage.Wmf.Primitive;

namespace SkiaWmfRenderer.Rendering;

static class Converters
{
    public static Encoding CharacterSetToEncoding(this CharacterSet characterSet)
    {
        var codePageId = characterSet switch
        {
            CharacterSet.ANSI_CHARSET
                // DEFAULT_CHARSET: Specifies a character set based on the current system locale; for example, when the system locale is United States English, the default character set is ANSI_CHARSET.
                or CharacterSet.DEFAULT_CHARSET => 1252,
            CharacterSet.OEM_CHARSET => 437,
            CharacterSet.SHIFTJIS_CHARSET => 932,
            CharacterSet.HANGUL_CHARSET => 949,
            CharacterSet.JOHAB_CHARSET => 1361,
            CharacterSet.GB2312_CHARSET => 936,
            CharacterSet.CHINESEBIG5_CHARSET => 950,
            CharacterSet.HEBREW_CHARSET => 1255,
            CharacterSet.ARABIC_CHARSET => 1256,
            CharacterSet.GREEK_CHARSET => 1253,
            CharacterSet.TURKISH_CHARSET => 1254,
            CharacterSet.BALTIC_CHARSET => 1257,
            CharacterSet.EASTEUROPE_CHARSET => 1250,
            CharacterSet.RUSSIAN_CHARSET => 1251,
            CharacterSet.THAI_CHARSET => 874,
            CharacterSet.VIETNAMESE_CHARSET => 1258,
            CharacterSet.SYMBOL_CHARSET => 42, // Symbol font is not a code page, but 42 is often used for Symbol font
            _ => 1252,
        };

        return Encoding.GetEncoding(codePageId);
    }

    public static SKColor ToSKColor(this WmfColor color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }

    public static SKPoint ToSKPoint(this WmfPoint point)
    {
        return new SKPoint(point.X, point.Y);
    }
}
// See https://aka.ms/new-console-template for more information

using HarfBuzzSharp;

using SkiaSharp;

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Buffer = HarfBuzzSharp.Buffer;

var symbolFontFile = Path.Join(AppContext.BaseDirectory, "StandardSymbolsPS.ttf");

var skTypeface =
    SKFontManager.Default.CreateTypeface(symbolFontFile);
Console.WriteLine($"Font='{symbolFontFile}' SKTypeface={skTypeface.FamilyName} GlyphCount={skTypeface.GlyphCount}");

var text = "p"; // 这里的 p 是 Symbol 字体中的 Pi 符号
char testChar = text[0];

Console.WriteLine($"ContainsGlyph('{testChar}')={skTypeface.ContainsGlyph(testChar)} {skTypeface.GetGlyph(testChar)}");

using var skBitmap = new SKBitmap(300, 300, SKColorType.Bgra8888, SKAlphaType.Premul);
skBitmap.Erase(SKColors.White);
using var skCanvas = new SKCanvas(skBitmap);

var skFont = skTypeface.ToFont(50);

using var skPaint = new SKPaint();
skPaint.Color = SKColors.Black;
skPaint.IsAntialias = true;

//skCanvas.DrawText(text, 50, 100, skFont, skPaint);

using (var buffer = new Buffer())
{
    buffer.AddUtf16(text);

    buffer.GuessSegmentProperties();
    buffer.Language = new Language(CultureInfo.CurrentCulture);

    var face = new HarfBuzzSharp.Face(GetTable);

    Blob? GetTable(Face f, Tag tag)
    {
        //Console.WriteLine($"GetTable...");

        //Console.WriteLine($"f.GlyphCount={f.GlyphCount}");
        /* 不能使用 f.GlyphCount，因为会导致死循环
           at Program.<<Main>$>g__GetTable|0_0(HarfBuzzSharp.Face, HarfBuzzSharp.Tag)
           at HarfBuzzSharp.DelegateProxies.ReferenceTableProxyImplementation(IntPtr, UInt32, Void*)
           at HarfBuzzSharp.HarfBuzzApi.hb_face_get_glyph_count(IntPtr)
           at HarfBuzzSharp.HarfBuzzApi.hb_face_get_glyph_count(IntPtr)
           at HarfBuzzSharp.Face.get_GlyphCount()
           at Program.<<Main>$>g__GetTable|0_0(HarfBuzzSharp.Face, HarfBuzzSharp.Tag)
           at HarfBuzzSharp.DelegateProxies.ReferenceTableProxyImplementation(IntPtr, UInt32, Void*)
           at HarfBuzzSharp.HarfBuzzApi.hb_face_get_glyph_count(IntPtr)
           at HarfBuzzSharp.HarfBuzzApi.hb_face_get_glyph_count(IntPtr)
           at HarfBuzzSharp.Face.get_GlyphCount()
           at Program.<<Main>$>g__GetTable|0_0(HarfBuzzSharp.Face, HarfBuzzSharp.Tag)
           at HarfBuzzSharp.DelegateProxies.ReferenceTableProxyImplementation(IntPtr, UInt32, Void*)
           at HarfBuzzSharp.HarfBuzzApi.hb_font_create(IntPtr)
           at HarfBuzzSharp.HarfBuzzApi.hb_font_create(IntPtr)
           at HarfBuzzSharp.Font..ctor(HarfBuzzSharp.Face)
         */

        var size = skTypeface.GetTableSize(tag);
        var data = Marshal.AllocCoTaskMem(size);
        if (skTypeface.TryGetTableData(tag, 0, size, data))
        {
            return new Blob(data, size, MemoryMode.ReadOnly, () => Marshal.FreeCoTaskMem(data));
        }
        else
        {
            return null;
        }
    }

    var font = new HarfBuzzSharp.Font(face);
    font.SetFunctionsOpenType();

    var tryGetGlyph = font.TryGetGlyph('p', out uint glyph);
    Console.WriteLine($"TryGetGlyph={tryGetGlyph} {glyph}");

    ReadOnlySpan<GlyphInfo> glyphInfoSpan = buffer.GetGlyphInfoSpan();
    foreach (var glyphInfo in glyphInfoSpan)
    {
        Console.WriteLine($"Before HarfBuzzSharp.Font.Shape Codepoint={glyphInfo.Codepoint}");
    }

    font.Shape(buffer);

    glyphInfoSpan = buffer.GetGlyphInfoSpan();
    foreach (var glyphInfo in glyphInfoSpan)
    {
        Console.WriteLine($"After HarfBuzzSharp.Font.Shape Codepoint={glyphInfo.Codepoint}");
    }

    ushort glyphId = (ushort) glyph;
    Span<byte> glyphByteSpan = stackalloc byte[sizeof(ushort)];
    MemoryMarshal.Write(glyphByteSpan, glyphId);

    var skTextBlob = SKTextBlob.Create(glyphByteSpan, SKTextEncoding.GlyphId, skFont);
    skCanvas.DrawText(skTextBlob, 200, 100, skPaint);
}

var outputFile = Path.Join(AppContext.BaseDirectory, $"2.png");

using (var outputStream = File.OpenWrite(outputFile))
{
    skBitmap.Encode(outputStream, SKEncodedImageFormat.Png, 100);
}

Console.Read();
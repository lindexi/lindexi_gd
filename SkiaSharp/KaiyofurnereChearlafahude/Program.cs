// See https://aka.ms/new-console-template for more information

using SkiaSharp;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;
using GlyphInfo = HarfBuzzSharp.GlyphInfo;
using System.Runtime.InteropServices;
using System.Globalization;
using System;
using System.Numerics;

var text = "123";
var fontName = "微软雅黑";

var skTypeface = SKTypeface.FromFamilyName(fontName);
var fontRenderingEmSize = 15;

var glyphInfoList = new List<TestGlyphInfo>();

using (var buffer = new Buffer())
{
    buffer.AddUtf16(text);
    buffer.GuessSegmentProperties();
    buffer.Language = new Language(CultureInfo.CurrentCulture);

    var face = new HarfBuzzSharp.Face(GetTable);

    Blob? GetTable(Face face, Tag tag)
    {
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

    font.Shape(buffer);

    font.GetScale(out var scaleX, out _);

    // Copy from https://github.com/AvaloniaUI/Avalonia
    // src\Skia\Avalonia.Skia\TextShaperImpl.cs
    var textScale = fontRenderingEmSize / (float) scaleX;

    var bufferLength = buffer.Length;

    var glyphInfos = buffer.GetGlyphInfoSpan();

    var glyphPositions = buffer.GetGlyphPositionSpan();

    for (var i = 0; i < bufferLength; i++)
    {
        var sourceInfo = glyphInfos[i];

        var glyphIndex = (ushort) sourceInfo.Codepoint;

        var glyphCluster = (int) sourceInfo.Cluster;

        var position = glyphPositions[i];

        var glyphAdvance = position.XAdvance * textScale;

        var offsetX = position.XOffset * textScale;

        var offsetY = -position.YOffset * textScale;

        glyphInfoList.Add(new TestGlyphInfo(glyphIndex, glyphCluster, glyphAdvance, (offsetX, offsetY)));
    }
}

var count = glyphInfoList.Count;

// Copy from https://github.com/AvaloniaUI/Avalonia
// src\Skia\Avalonia.Skia\GlyphRunImpl.cs
var glyphIndices = new ushort[count];
var renderGlyphPositions = new SKPoint[count];

var currentX = 0.0;

for (int i = 0; i < count; i++)
{
    var glyphInfo = glyphInfoList[i];
    var offset = glyphInfo.GlyphOffset;

    glyphIndices[i] = glyphInfo.GlyphIndex;

    renderGlyphPositions[i] = new SKPoint((float) (currentX + offset.OffsetX), (float) offset.OffsetY);

    currentX += glyphInfoList[i].GlyphAdvance;
}

// Ideally the requested edging should be passed to the glyph run.
// Currently the edging is computed dynamically inside the drawing context, so we can't know it in advance.
// But the bounds depends on the edging: for now, always use SubpixelAntialias so we have consistent values.
// The resulting bounds may be shifted by 1px on some fonts:
// "F" text with Inter size 14 has a 0px left bound with SubpixelAntialias but 1px with Antialias.

var edging = SKFontEdging.SubpixelAntialias;

var skFont = new SKFont(skTypeface, fontRenderingEmSize);
skFont.Hinting = SKFontHinting.Full;
skFont.Edging = edging;
skFont.Subpixel = edging != SKFontEdging.Alias;

var runBounds = new SKRect();
var glyphBounds = new SKRect[count];
skFont.GetGlyphWidths(glyphIndices, null, glyphBounds);

var glyphRunBounds = new SKRect[count];

var baselineOrigin = new SKPoint(0, 0);

currentX = 0;

for (var i = 0; i < count; i++)
{
    var gBounds = glyphBounds[i];
    var glyphInfo = glyphInfoList[i];
    var advance = glyphInfo.GlyphAdvance;

    glyphRunBounds[i] = new SKRect((float) (currentX + gBounds.Left), baselineOrigin.Y + gBounds.Top, gBounds.Width,
        gBounds.Height);

    runBounds.Union(glyphRunBounds[i]);

    currentX += advance;
}

if (runBounds.Left < 0)
{
    runBounds.Offset(-runBounds.Left, 0);
}

runBounds.Offset(baselineOrigin.X, 0);

var builder = new SKTextBlobBuilder();
var runBuffer = builder.AllocatePositionedRun(skFont, glyphIndices.Length);
runBuffer.SetPositions(renderGlyphPositions);
runBuffer.SetGlyphs(glyphIndices);

var textBlob = builder.Build();

var skPaint = new SKPaint(skFont)
{
    Color = SKColors.Black,
    TextSize = fontRenderingEmSize
};

var width = 1920;
var height = 1080;

using var skBitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888));
using (var skCanvas = new SKCanvas(skBitmap))
{
    skCanvas.Clear(SKColors.White);

    skCanvas.Translate(100, 100);
    skPaint.Style = SKPaintStyle.Stroke;
    skCanvas.DrawRect(0, 0, 500, 500, skPaint);

    skCanvas.DrawText(textBlob, baselineOrigin.X, baselineOrigin.Y, skPaint);
}

var file = "1.png";

var skData = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
using var fileStream = File.OpenWrite(file);
skData.SaveTo(fileStream);

Console.WriteLine("Hello, World!");


readonly record struct TestGlyphInfo(ushort GlyphIndex, int GlyphCluster, double GlyphAdvance, (float OffsetX, float OffsetY) GlyphOffset = default);
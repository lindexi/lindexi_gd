using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;

using HarfBuzzSharp;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;
using SkiaSharp;

using Buffer = HarfBuzzSharp.Buffer;
using Font = HarfBuzzSharp.Font;

namespace LightTextEditorPlus.Platform;

class SkiaCharInfoMeasurer : ICharInfoMeasurer
{
    public SkiaCharInfoMeasurer(SkiaTextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    private SkiaTextEditor TextEditor { get; }

    public CharInfoMeasureResult MeasureCharInfo(in CharInfo charInfo)
    {
        var skFontManager = SKFontManager.Default;
        var skTypeface = skFontManager.MatchFamily("微软雅黑");

        using SKPaint skPaint = new SKPaint();
        skPaint.Typeface = skTypeface;
        skPaint.TextSize = (float) charInfo.RunProperty.FontSize;

        var textAdvances = skPaint.GetGlyphWidths(charInfo.CharObject.ToText(), out var skBounds);
        // 使用 GetGlyphWidths 布局也能达到效果，但是其布局效果本身不佳
        // 暂时没有找到如何对齐基线
        if (skBounds != null && skBounds.Length > 0 && false)
        {
            // 为什么实际渲染会感觉超过 11 的值？这是因为 DrawText 的 Point 给的是最下方的坐标，而不是最上方的坐标
            // 字号是 15 时，测量返回的高度是 11 的值。这是因为这个 11 指的是字符渲染高度
            //// 这里测量的高度是 11 的值，然而实际渲染是超过 11 的值
            return new CharInfoMeasureResult(skBounds[0].ToRect() with
            {
                X = 0,
                Y = 0,
                Width = textAdvances[0],
                //Height = skBounds[0].Height
            });
        }

        var asset = skTypeface.OpenStream(out var trueTypeCollectionIndex);
        var size = asset.Length;
        var memoryBase = asset.GetMemoryBase();

        using var blob = new Blob(memoryBase, size, MemoryMode.ReadOnly, () => asset.Dispose());
        blob.MakeImmutable();

        using var face = new Face(blob, (uint) trueTypeCollectionIndex);
        face.UnitsPerEm = skTypeface.UnitsPerEm;

        var fontSize = charInfo.RunProperty.FontSize;

        using var font = new Font(face);
        font.SetFunctionsOpenType();
        font.GetScale(out var x, out var y);

        float glyphScale = (float) (fontSize / x);

        using var buffer = new Buffer();
        buffer.AddUtf32("123微软雅黑bfg");

        buffer.Direction = Direction.LeftToRight;
        buffer.Script = Script.Han;
        buffer.Language = new Language("en");

        buffer.GuessSegmentProperties();

        font.Shape(buffer);

        foreach (GlyphInfo glyphInfo in buffer.GlyphInfos)
        {
            uint glyphInfoCodepoint = glyphInfo.Codepoint;
        }

        var length = 0f;
        Rect bounds = Rect.Zero;
        foreach (var glyphPosition in buffer.GlyphPositions)
        {
            var left = glyphPosition.XOffset * glyphScale;
            var top = glyphPosition.YOffset * glyphScale;
            var width = glyphPosition.XAdvance * glyphScale;
            var height = glyphPosition.YAdvance * glyphScale;

            // 预计 height 就是 0 的值
            // https://github.com/harfbuzz/harfbuzz/discussions/4827
            if (height == 0)
            {
                height = (float) fontSize;
            }

            bounds = new Rect(left, top, width, height);

            length += glyphPosition.XOffset * glyphScale + glyphPosition.XAdvance * glyphScale;
        }

#if DEBUG
        GC.KeepAlive(length); // 调试代码，仅用于方便在此调试获取其长度/宽度
#endif

        return new CharInfoMeasureResult(bounds);
    }
}
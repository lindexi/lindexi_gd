using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;

using HarfBuzzSharp;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
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
        // 此逻辑只有空行测量才会进入

        SkiaTextRunProperty skiaTextRunProperty = charInfo.RunProperty.AsSkiaRunProperty();

        RenderingRunPropertyInfo renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo(charInfo.CharObject.CodePoint);

        var skTypeface = renderingRunPropertyInfo.Typeface;

        SKPaint skPaint = renderingRunPropertyInfo.Paint;

        SKFont skFont = renderingRunPropertyInfo.Font;
        float baselineY = -skFont.Metrics.Ascent;
        var textAdvances = skPaint.GetGlyphWidths(charInfo.CharObject.ToText(), out var skBounds);
        // 使用 GetGlyphWidths 布局也能达到效果，但是其布局效果本身不佳
        // 暂时没有找到如何对齐基线
        // 但是在绘制渲染时，自动带上了基线对齐，因此保持 Y 坐标为 0 即可
        if (skBounds != null && skBounds.Length > 0)
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
                // 测量的高度是 11 的值，却设置为字体大小 15 的值。刚好渲染 123微软雅黑 时，自动让 123 对齐基线
                Height = skPaint.TextSize // todo 使用 FontCharHelper 的计算方法
            }, baselineY);
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
        font.SetFunctionsOpenType();

        float glyphScale = (float) (fontSize / x);

        using var buffer = new Buffer();
        buffer.AddUtf32(charInfo.CharObject.ToText());

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
        TextRect bounds = TextRect.Zero;
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

            bounds = new TextRect(left, top, width, height);

            length += glyphPosition.XOffset * glyphScale + glyphPosition.XAdvance * glyphScale;
        }

#if DEBUG
        GC.KeepAlive(length); // 调试代码，仅用于方便在此调试获取其长度/宽度
#endif

        return new CharInfoMeasureResult(bounds, baselineY);
    }
}

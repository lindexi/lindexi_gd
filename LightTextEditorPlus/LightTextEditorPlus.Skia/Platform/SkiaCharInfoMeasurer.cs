using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;

using HarfBuzzSharp;
using LightTextEditorPlus.Core.Primitive;
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

        var asset = skTypeface.OpenStream(out var trueTypeCollectionIndex);
        var size = asset.Length;
        var memoryBase = asset.GetMemoryBase();

        var blob = new Blob(memoryBase, size, MemoryMode.ReadOnly, () => asset.Dispose());
        blob.MakeImmutable();

        var face = new Face(blob, (uint) trueTypeCollectionIndex);
        face.UnitsPerEm = skTypeface.UnitsPerEm;

        var fontSize = charInfo.RunProperty.FontSize;

        var font = new Font(face);
        font.SetFunctionsOpenType();
        font.GetScale(out var x, out var y);

        float glyphScale =(float) (fontSize / x);

        using var buffer = new Buffer();
        buffer.AddUtf32(charInfo.CharObject.ToText());

        buffer.Direction = Direction.LeftToRight;
        buffer.Script = Script.Han;

        font.Shape(buffer);

        var length = 0f;
        Rect bounds = Rect.Zero;
        foreach (var glyphPosition in buffer.GlyphPositions)
        {
            var left = glyphPosition.XOffset * glyphScale;
            var top = glyphPosition.YOffset * glyphScale;
            var width = glyphPosition.XAdvance * glyphScale;
            var height = glyphPosition.YAdvance * glyphScale;

            bounds = new Rect(left, top, width, height);

            length += glyphPosition.XOffset * glyphScale + glyphPosition.XAdvance * glyphScale;
        }

        return new CharInfoMeasureResult(bounds);
    }
}
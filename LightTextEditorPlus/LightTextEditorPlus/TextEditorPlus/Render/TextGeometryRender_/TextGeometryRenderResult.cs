using System.Windows;
using System.Windows.Media;

namespace LightTextEditorPlus.TextEditorPlus.Render
{
    readonly struct TextGeometryRenderResult
    {
        public TextGeometryRenderResult(Geometry textGeometry, Size textGeometrySize, TextCharGlyphData? textCharGlyphData)
        {
            TextGeometry = textGeometry;
            TextGeometrySize = textGeometrySize;
            TextCharGlyphData = textCharGlyphData;
        }

        public Geometry TextGeometry { get; }
        public Size TextGeometrySize { get; }
        public TextCharGlyphData? TextCharGlyphData { get; }

        public void Deconstruct(out Geometry textGeometry, out Size textGeometrySize, out
            TextCharGlyphData? textCharGlyphData)
        {
            textGeometry = TextGeometry;
            textGeometrySize = TextGeometrySize;
            textCharGlyphData = TextCharGlyphData;
        }

        public static implicit operator TextGeometryRenderResult((Geometry textGeometry, Size textGeometrySize, TextCharGlyphData textCharGlyphData) result)
        {
            return new TextGeometryRenderResult(result.textGeometry, result.textGeometrySize, result.textCharGlyphData);
        }

        public static implicit operator (Geometry textGeometry, Size textGeometrySize, TextCharGlyphData?
            textCharGlyphData)(TextGeometryRenderResult result)
        {
            return (result.TextGeometry, result.TextGeometrySize, result.TextCharGlyphData);
        }
    }
}
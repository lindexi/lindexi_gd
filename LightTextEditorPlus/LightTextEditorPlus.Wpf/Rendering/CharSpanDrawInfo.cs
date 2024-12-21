using System.Windows.Media;

using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Rendering;

record CharSpanDrawInfo(ushort GlyphIndex, GlyphTypeface GlyphTypeface, char CurrentChar, CharData CharData);
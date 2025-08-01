using System.Windows.Media;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Rendering;

record CharSpanDrawInfo(ushort GlyphIndex, GlyphTypeface GlyphTypeface, Utf32CodePoint CurrentChar, CharData CharData);

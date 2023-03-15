using System.Collections.Generic;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Media;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Rendering;

/// <summary>
/// 文本渲染器基类
/// </summary>
abstract class TextRenderBase
{
    public abstract DrawingVisual Render(RenderInfoProvider renderInfoProvider, TextEditor textEditor);

    protected static XmlLanguage DefaultXmlLanguage { get; } =
        XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);
}

record LineDrawCharSpanInfo(List<ushort> glyphIndices, List<double> advanceWidths, List<char> characters,Point startPoint,double maxLineRenderHeight, GlyphTypeface glyphTypeface);

record CharSpanDrawInfo(ushort glyphIndex, GlyphTypeface glyphTypeface,char CurrentChar,CharData charData);
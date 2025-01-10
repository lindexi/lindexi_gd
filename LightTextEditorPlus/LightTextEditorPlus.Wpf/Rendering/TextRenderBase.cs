using System.Collections.Generic;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Media;
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
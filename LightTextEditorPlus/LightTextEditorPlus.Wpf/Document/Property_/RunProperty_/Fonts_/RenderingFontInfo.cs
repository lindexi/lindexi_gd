using System.Windows.Media;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 渲染时的字体信息
/// </summary>
/// <param name="GlyphTypeface"></param>
/// <param name="RenderingFontFamily"></param>
public readonly record struct RenderingFontInfo(GlyphTypeface GlyphTypeface, FontFamily RenderingFontFamily);
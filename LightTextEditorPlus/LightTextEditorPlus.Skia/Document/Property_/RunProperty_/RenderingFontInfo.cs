using SkiaSharp;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 渲染时的字体信息
/// </summary>
internal readonly record struct RenderingFontInfo(SKTypeface Typeface);
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core;

readonly record struct SkiaTextRenderArgument
{
    public required SKCanvas Canvas { get; init; }
    public required RenderInfoProvider RenderInfoProvider { get; init; }
    public required TextRect RenderBounds { get; init; }

    /// <summary>
    /// 可见范围，为空则代表需要全文档渲染
    /// </summary>
    public TextRect? Viewport { get; init; }
}
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Rendering.Core;

readonly record struct SkiaTextRenderResult
{
    public required TextRect RenderBounds { get; init; }
}
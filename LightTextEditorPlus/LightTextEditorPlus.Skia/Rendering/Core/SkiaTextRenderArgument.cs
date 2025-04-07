using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core;

readonly record struct SkiaTextRenderArgument
{
    public required SKCanvas Canvas { get; init; }
    public required RenderInfoProvider RenderInfoProvider { get; init; }
    public required TextRect RenderBounds { get; init; }
}
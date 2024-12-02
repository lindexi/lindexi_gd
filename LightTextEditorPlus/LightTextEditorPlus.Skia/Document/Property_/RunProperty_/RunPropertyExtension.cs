using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

static class RunPropertyExtension
{
    public static SkiaTextRunProperty AsSkiaRunProperty(this IReadOnlyRunProperty runProperty) => (SkiaTextRunProperty) runProperty;
}
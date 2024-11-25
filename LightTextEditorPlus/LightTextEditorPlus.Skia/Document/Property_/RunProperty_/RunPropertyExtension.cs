using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

static class RunPropertyExtension
{
    public static SkiaTextRunProperty AsRunProperty(this IReadOnlyRunProperty runProperty) => (SkiaTextRunProperty) runProperty;
}
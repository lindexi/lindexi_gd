using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

static class RunPropertyExtension
{
    public static RunProperty AsRunProperty(this IReadOnlyRunProperty runProperty) => (RunProperty) runProperty;

    public static RunProperty AsIRunProperty(this IReadOnlyRunProperty runProperty) => runProperty.AsRunProperty();
}
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Document;

internal class SkiaPlatformRunPropertyCreator : IPlatformRunPropertyCreator
{
    public SkiaPlatformRunPropertyCreator(SkiaPlatformResourceManager skiaPlatformResourceManager)
    {
        _skiaPlatformResourceManager = skiaPlatformResourceManager;
    }

    private readonly SkiaPlatformResourceManager _skiaPlatformResourceManager;


    public IReadOnlyRunProperty GetDefaultRunProperty()
    {
        return new SkiaTextRunProperty(_skiaPlatformResourceManager);
    }
}
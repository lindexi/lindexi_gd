using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Document;

internal class SkiaPlatformRunPropertyCreator : PlatformRunPropertyCreatorBase<SkiaTextRunProperty>
{
    public SkiaPlatformRunPropertyCreator(SkiaPlatformResourceManager skiaPlatformResourceManager)
    {
        _skiaPlatformResourceManager = skiaPlatformResourceManager;
    }

    private readonly SkiaPlatformResourceManager _skiaPlatformResourceManager;

    protected override SkiaTextRunProperty OnGetDefaultRunProperty()
    {
        return new SkiaTextRunProperty(_skiaPlatformResourceManager);
    }
}
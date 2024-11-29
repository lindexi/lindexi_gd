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

    public override IReadOnlyRunProperty ToPlatformRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty)
    {
        if (baseRunProperty is SkiaTextRunProperty skiaTextRunProperty)
        {
            return skiaTextRunProperty;
        }
        else
        {
            // 让底层去抛出异常
            return base.ToPlatformRunProperty(charObject, baseRunProperty);
        }
    }

    protected override SkiaTextRunProperty OnGetDefaultRunProperty()
    {
        return new SkiaTextRunProperty(_skiaPlatformResourceManager);
    }
}
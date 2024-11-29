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

    public override IReadOnlyRunProperty GetRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty)
    {
        if (baseRunProperty is SkiaTextRunProperty skiaTextRunProperty)
        {
            return skiaTextRunProperty;
        }
        else
        {
            // 非当前平台支持的属性
            throw new NotSupportedException(); // todo 填充内容
        }
    }

    protected override SkiaTextRunProperty OnGetDefaultRunProperty()
    {
        return new SkiaTextRunProperty(_skiaPlatformResourceManager);
    }
}
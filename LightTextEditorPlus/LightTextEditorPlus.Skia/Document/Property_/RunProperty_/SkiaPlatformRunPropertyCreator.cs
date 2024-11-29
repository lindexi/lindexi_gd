using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Document;

internal class SkiaPlatformRunPropertyCreator : PlatformRunPropertyCreatorBase<SkiaTextRunProperty>
{
    public SkiaPlatformRunPropertyCreator(SkiaPlatformResourceManager skiaPlatformResourceManager,ITextLogger logger)
    {
        _skiaPlatformResourceManager = skiaPlatformResourceManager;
        _logger = logger;
    }

    private readonly SkiaPlatformResourceManager _skiaPlatformResourceManager;
    private readonly ITextLogger _logger;

    public override IReadOnlyRunProperty ToPlatformRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty)
    {
        if (baseRunProperty is SkiaTextRunProperty skiaTextRunProperty)
        {
            if (!ReferenceEquals(skiaTextRunProperty.ResourceManager, _skiaPlatformResourceManager))
            {
                // 是从其他平台创建的？
                // 尝试兼容
                skiaTextRunProperty = skiaTextRunProperty with
                {
                    ResourceManager = _skiaPlatformResourceManager
                };
            }

            bool canFontSupportChar = _skiaPlatformResourceManager.CanFontSupportChar(skiaTextRunProperty, charObject);
            if (!canFontSupportChar)
            {
                _logger.LogWarning($"当前字体 {skiaTextRunProperty.FontName} 不支持字符 {charObject.ToText()}");
                // todo 进行字体回滚。完成字体回滚才能删除下面代码
                throw new NotSupportedException($"当前字体 {skiaTextRunProperty.FontName} 不支持字符 {charObject.ToText()}");
            }

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
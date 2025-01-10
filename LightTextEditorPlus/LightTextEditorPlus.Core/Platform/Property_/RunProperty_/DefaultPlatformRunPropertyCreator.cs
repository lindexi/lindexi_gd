using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 默认的平台相关文本字符属性创建器
/// </summary>
public class DefaultPlatformRunPropertyCreator : PlatformRunPropertyCreatorBase<LayoutOnlyRunProperty>
{
    ///// <inheritdoc />
    //protected override LayoutOnlyRunProperty OnBuildNewProperty(Action<IReadOnlyRunProperty> config,
    //    LayoutOnlyRunProperty baseRunProperty)
    //{
    //    var runProperty = new LayoutOnlyRunProperty(baseRunProperty);
    //    config(runProperty);
    //    return runProperty;
    //}

    /// <inheritdoc />
    protected override LayoutOnlyRunProperty OnGetDefaultRunProperty()
    {
        return new LayoutOnlyRunProperty();
    }
}
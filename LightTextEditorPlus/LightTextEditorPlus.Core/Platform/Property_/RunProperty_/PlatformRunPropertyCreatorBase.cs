using System;

using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 平台相关的文本字符属性创建器基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class PlatformRunPropertyCreatorBase<T> : IPlatformRunPropertyCreator
    where T : IReadOnlyRunProperty
{
    /// <inheritdoc />
    public IReadOnlyRunProperty GetDefaultRunProperty()
    {
        return OnGetDefaultRunProperty();
    }

    /// <inheritdoc />
    public virtual IReadOnlyRunProperty ToPlatformRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty)
    {
        if (baseRunProperty is not T)
        {
            throw CreateRunPropertyTypeNotSupportedException(baseRunProperty);
        }

        return baseRunProperty;
    }

    /// <inheritdoc />
    public IReadOnlyRunProperty UpdateMarkerRunProperty(IReadOnlyRunProperty? markerRunProperty,
        IReadOnlyRunProperty styleRunProperty)
    {
        if (styleRunProperty is not T style)
        {
            throw CreateRunPropertyTypeNotSupportedException(styleRunProperty);
        }

        if (markerRunProperty is not T marker)
        {
            if (markerRunProperty is null)
            {
                marker = default!;
            }
            else
            {
                throw CreateRunPropertyTypeNotSupportedException(markerRunProperty);
            }
        }

        return OnUpdateMarkerRunProperty(marker, style);
    }

    /// <inheritdoc cref="UpdateMarkerRunProperty"/>
    protected abstract T OnUpdateMarkerRunProperty(T? markerRunProperty, T styleRunProperty);

    /// <inheritdoc cref="GetDefaultRunProperty"/>
    protected abstract T OnGetDefaultRunProperty();

    /// <summary>
    /// 创建不支持的字符属性异常
    /// </summary>
    /// <param name="runProperty"></param>
    /// <exception cref="NotSupportedException"></exception>
    private static NotSupportedException CreateRunPropertyTypeNotSupportedException(IReadOnlyRunProperty? runProperty) => new NotSupportedException($"传入了非当前平台所能支持的字符属性。当前平台属性类型： {typeof(T).FullName}；传入的字符属性类型：{runProperty?.GetType().FullName ?? "<null>"}");
}
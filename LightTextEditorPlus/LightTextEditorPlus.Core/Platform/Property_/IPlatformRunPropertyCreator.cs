using LightTextEditorPlus.Core.Document;

using System;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 平台相关的字符属性创建器
/// </summary>
public interface IPlatformRunPropertyCreator
{
    /// <summary>
    /// 获取默认的字符属性
    /// </summary>
    /// <returns></returns>
    IReadOnlyRunProperty GetDefaultRunProperty();

    /// <summary>
    /// 获取字符属性。需要处理字符的字体降级
    /// 这个方法包含两个功能：
    /// 1. 如果传入的平台属性不属于当前的平台属性，则自动进行处理或记录或报告错误
    /// 2. 如果给定字符不能满足当前的平台属性，则自动处理字符的字体降级、或记录或报告错误
    /// </summary>
    /// <param name="charObject"></param>
    /// <param name="baseRunProperty"></param>
    /// <returns></returns>
    /// todo 改名
    IReadOnlyRunProperty GetRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty);
}

/// <summary>
/// 平台相关的文本字符属性创建器基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class PlatformRunPropertyCreatorBase<T> : IPlatformRunPropertyCreator
    where T : IReadOnlyRunProperty
{
    ///// <inheritdoc />
    //public IReadOnlyRunProperty BuildNewProperty(Action<IReadOnlyRunProperty> config,
    //    IReadOnlyRunProperty baseRunProperty)
    //{
    //    return OnBuildNewProperty(config, (T)baseRunProperty);
    //}

    ///// <inheritdoc cref="BuildNewProperty"/>
    //protected abstract T OnBuildNewProperty(Action<IReadOnlyRunProperty> config, T baseRunProperty);

    /// <inheritdoc />
    public IReadOnlyRunProperty GetDefaultRunProperty()
    {
        return OnGetDefaultRunProperty();
    }

    /// <inheritdoc />
    public virtual IReadOnlyRunProperty GetRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty)
    {
        if (baseRunProperty is not T)
        {
            throw new NotSupportedException($"传入了非当前平台所能支持的字符属性。当前平台属性类型： {typeof(T).FullName}；传入的字符属性类型：{baseRunProperty?.GetType().FullName ?? "<null>"}");
        }

        return baseRunProperty;
    }

    /// <inheritdoc cref="GetDefaultRunProperty"/>
    protected abstract T OnGetDefaultRunProperty();
}

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
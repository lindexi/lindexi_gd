using LightTextEditorPlus.Core.Document;

using System;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 平台相关的字符属性创建器
/// </summary>
public interface IPlatformRunPropertyCreator
{
    /// <summary>
    /// 基于原有的只读属性创建出新的字符属性
    /// </summary>
    /// <returns></returns>
    IReadOnlyRunProperty BuildNewProperty(Action<IReadOnlyRunProperty> config,IReadOnlyRunProperty baseRunProperty);

    /// <summary>
    /// 获取默认的字符属性
    /// </summary>
    /// <returns></returns>
    /// 允许跨多个 TextEditor 使用
    IReadOnlyRunProperty GetDefaultRunProperty();
}

public abstract class PlatformRunPropertyCreatorBase<T> : IPlatformRunPropertyCreator
    where T : IReadOnlyRunProperty
{
    public IReadOnlyRunProperty BuildNewProperty(Action<IReadOnlyRunProperty> config, IReadOnlyRunProperty baseRunProperty)
    {
        return OnBuildNewProperty(config, (T) baseRunProperty);
    }

    protected abstract T OnBuildNewProperty(Action<IReadOnlyRunProperty> config, T baseRunProperty);

    public IReadOnlyRunProperty GetDefaultRunProperty()
    {
        return OnGetDefaultRunProperty();
    }

    protected abstract T OnGetDefaultRunProperty();
}

public class DefaultPlatformRunPropertyCreator : PlatformRunPropertyCreatorBase<RunProperty>
{
    protected override RunProperty OnBuildNewProperty(Action<IReadOnlyRunProperty> config, RunProperty baseRunProperty)
    {
        var runProperty = new RunProperty(baseRunProperty);
        config(runProperty);
        return runProperty;
    }

    protected override RunProperty OnGetDefaultRunProperty()
    {
        return new RunProperty();
    }
}

public interface IParagraphPropertyCreator
{

}
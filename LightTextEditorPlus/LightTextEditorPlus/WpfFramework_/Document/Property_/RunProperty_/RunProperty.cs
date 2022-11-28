using System.Windows.Media;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

public class RunProperty : LayoutOnlyRunProperty
{
    internal RunProperty(RunPropertyPlatformManager runPropertyPlatformManager, RunProperty? styleRunProperty = null) : base(styleRunProperty)
    {
        RunPropertyPlatformManager = runPropertyPlatformManager;
        StyleRunProperty = styleRunProperty;
    }

    private RunPropertyPlatformManager RunPropertyPlatformManager { get; }

    /// <summary>
    /// 继承样式里的属性
    /// </summary>
    private LayoutOnlyRunProperty? StyleRunProperty { get; }
}

/// <summary>
/// 表示一个不可变的画刷
/// </summary>
/// 要是还有人去拿属性去改，那我也救不了了
public class ImmutableBrush : ImmutableRunPropertyValue<Brush>
{
    public ImmutableBrush(Brush value) : base(value)
    {
    }
}
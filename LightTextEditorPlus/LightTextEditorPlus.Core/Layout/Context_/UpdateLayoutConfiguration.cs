using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 更新布局的配置
/// </summary>
public readonly record struct UpdateLayoutConfiguration
{
    /// <summary>
    /// 是否需要清除字符大小，因为 <see cref="ArrangingType"/> 发生了变化。如横排竖排切换，此时字符的大小需要重新计算出新的值
    /// </summary>
    /// 按照现在的设计，横竖排不仅仅只是倒换宽高属性，里面的属性值也会发生变化，如高度需要加上计算的渲染高度，如 [WPF 探索 Skia 的竖排文本渲染的字符高度 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18815810 )
    public bool ShouldClearCharSizeForArrangingTypeChanged { get; init; }
}
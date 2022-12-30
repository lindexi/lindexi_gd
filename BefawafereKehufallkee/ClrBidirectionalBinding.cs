using System.ComponentModel;

namespace BefawafereKehufallkee;

/// <summary>
/// 实现两个 CLR 属性的双向绑定
/// </summary>
public class ClrBidirectionalBinding
{
    public ClrBidirectionalBinding(ClrBindingPropertyContext source, ClrBindingPropertyContext target,
        BindingDirection direction = BindingDirection.TwoWay,
        IClrValueConverter? converter = null,
        BindingInitMode initMode = BindingInitMode.SourceToTarget)
    {
        Source = source;
        Target = target;
        Direction = direction;
        InitMode = initMode;

        ValueConverter = converter;


    }

    public ClrBindingPropertyContext Source { get; }
    public ClrBindingPropertyContext Target { get; }

    public IClrValueConverter? ValueConverter { get; }

    /// <summary>
    /// 绑定方向，默认 TwoWay。  
    /// </summary>
    public BindingDirection Direction { get; }

    /// <summary>
    /// 初始化模式，默认 SourceToTarget。
    /// SourceToTarget：初始值以 Source 为准；TargetToSource：初始值以 Target 为准。
    /// </summary>
    public BindingInitMode InitMode { get; }

    internal bool IsAlive() => Source.BindableObjectWeakReference.TryGetTarget(out _) &&
                               Target.BindableObjectWeakReference.TryGetTarget(out _);

}
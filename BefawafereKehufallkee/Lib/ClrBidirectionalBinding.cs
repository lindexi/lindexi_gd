using System.ComponentModel;

namespace BefawafereKehufallkee;

/// <summary>
/// 实现两个 CLR 属性的双向绑定
/// </summary>
public class ClrBidirectionalBinding
{
    public ClrBidirectionalBinding(ClrBindingPropertyContext source, ClrBindingPropertyContext target,
        BindingDirection direction = BindingDirection.TwoWay,
        //IClrValueConverter? converter = null,
        BindingInitMode initMode = BindingInitMode.SourceToTarget)
    {
        Source = source;
        Target = target;
        //Direction = direction;
        //InitMode = initMode;

        //ValueConverter = converter;

        if (!source.BindableObjectWeakReference.TryGetTarget(out var sourceObject)
            || !target.BindableObjectWeakReference.TryGetTarget(out var targetObject))
        {
            return;
        }

        if (sourceObject is not INotifyPropertyChanged sourceNotifyPropertyChanged)
        {
            throw new ArgumentException("Source not implement interface INotifyPropertyChanged");
        }

        sourceNotifyPropertyChanged.PropertyChanged += Source_OnPropertyChanged;
        bool needSourceGetter = true; // 这是一定的，绑定的时候，需要 Source 的 Getter 方法
        bool needSourceSetter = false; // 如果不是 TwoWay 或者是 TargetToSource 那就不需要 Source 的 Setter 方法
        bool needTargetGetter = false; // 如果不是  TwoWay 或者是 TargetToSource 那就不需要 Target 的 Getter 方法
        bool needTargetSetter = true; // 这是一定的，绑定的时候，需要 Target 的 Setter 方法

        if (direction == BindingDirection.OneWay)
        {
            // 单向，不需要监听 Target 的事件
        }
        else if (direction == BindingDirection.TwoWay)
        {
            needSourceSetter = true;
            needTargetGetter = true;

            // 需要监听 Target 的事件
            if (targetObject is not INotifyPropertyChanged targetNotifyPropertyChanged)
            {
                throw new ArgumentException("Target not implement interface INotifyPropertyChanged");
            }

            targetNotifyPropertyChanged.PropertyChanged += Target_OnPropertyChanged;
        }

        // 初始化赋值
        switch (initMode)
        {
            case BindingInitMode.SourceToTarget:
                needSourceGetter = true;
                needTargetSetter = true;
                break;
            case BindingInitMode.None:
                break;
            case BindingInitMode.TargetToSource:
                needTargetGetter = true;
                needSourceSetter = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (needSourceGetter && source.InternalPropertyGetter is null)
        {
            throw new ArgumentNullException("source.PropertyGetter");
        }

        if (needSourceSetter && source.InternalPropertySetter is null)
        {
            throw new ArgumentNullException("source.PropertySetter");
        }

        if (needTargetGetter && target.InternalPropertyGetter is null)
        {
            throw new ArgumentNullException("target.PropertyGetter");
        }

        if (needTargetSetter && target.InternalPropertySetter is null)
        {
            throw new ArgumentNullException("target.PropertySetter");
        }

        switch (initMode)
        {
            case BindingInitMode.SourceToTarget:
                SetSourceToTarget();
                break;
            case BindingInitMode.None:
                break;
            case BindingInitMode.TargetToSource:
                SetTargetToSource();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static void SetBinding(ClrBindingPropertyContext source, ClrBindingPropertyContext target,
        BindingDirection direction = BindingDirection.TwoWay,
        //IClrValueConverter? converter = null,
        BindingInitMode initMode = BindingInitMode.SourceToTarget) =>
        // 建立绑定关系即可，不需要存放此对象
        _ = new ClrBidirectionalBinding(source, target, direction, initMode);

    public static void SetBinding(object source, string sourcePropertyPath,
        object target, string targetPropertyPath, BindingDirection direction = BindingDirection.TwoWay,
        BindingInitMode initMode = BindingInitMode.SourceToTarget,
        PropertyGetter? sourcePropertyGetter = null, PropertySetter? sourcePropertySetter = null,
        PropertyGetter? targetPropertyGetter = null, PropertySetter? targetPropertySetter = null)
        => SetBinding
        (
            new ClrBindingPropertyContext(source, sourcePropertyPath, sourcePropertyGetter, sourcePropertySetter),
            new ClrBindingPropertyContext(target, targetPropertyPath, targetPropertyGetter, targetPropertySetter),
            direction,
            initMode
        );

    public static void SetOneWayBinding(object source, string sourcePropertyPath,
        object target, string targetPropertyPath, BindingInitMode initMode = BindingInitMode.SourceToTarget,
        PropertyGetter? sourcePropertyGetter = null,
        PropertySetter? targetPropertySetter = null) =>
        SetBinding(new ClrBindingPropertyContext(source, sourcePropertyPath, sourcePropertyGetter),
            new ClrBindingPropertyContext(target, targetPropertyPath, propertySetter: targetPropertySetter),
            BindingDirection.OneWay, initMode);

    private void Source_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInnerSet)
        {
            return;
        }

        if (string.Equals(e.PropertyName, Source.Path, StringComparison.Ordinal))
        {
            SetSourceToTarget();
        }
    }

    private void Target_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isInnerSet)
        {
            return;
        }

        if (string.Equals(e.PropertyName, Target.Path, StringComparison.Ordinal))
        {
            SetTargetToSource();
        }
    }

    private void SetSourceToTarget()
    {
        _isInnerSet = true;

        try
        {
            if (!Source.BindableObjectWeakReference.TryGetTarget(out var sourceObject)
                || !Target.BindableObjectWeakReference.TryGetTarget(out var targetObject))
            {
                BreakBinding();
                return;
            }

            var sourceValue = Source.InternalPropertyGetter!.Invoke(sourceObject);
            Target.InternalPropertySetter!.Invoke(targetObject, sourceValue);
        }
        finally
        {
            _isInnerSet = false;
        }
    }

    private void SetTargetToSource()
    {
        _isInnerSet = true;

        try
        {
            if (!Source.BindableObjectWeakReference.TryGetTarget(out var sourceObject)
                || !Target.BindableObjectWeakReference.TryGetTarget(out var targetObject))
            {
                BreakBinding();
                return;
            }

            var targetValue = Target.InternalPropertyGetter!.Invoke(targetObject);
            Source.InternalPropertySetter!.Invoke(sourceObject, targetValue);
        }
        finally
        {
            _isInnerSet = false;
        }
    }

    private bool _isInnerSet;

    public ClrBindingPropertyContext Source { get; }
    public ClrBindingPropertyContext Target { get; }

    //public IClrValueConverter? ValueConverter { get; }

    ///// <summary>
    ///// 绑定方向，默认 TwoWay。  
    ///// </summary>
    //public BindingDirection Direction { get; }

    ///// <summary>
    ///// 初始化模式，默认 SourceToTarget。
    ///// SourceToTarget：初始值以 Source 为准；TargetToSource：初始值以 Target 为准。
    ///// </summary>
    //public BindingInitMode InitMode { get; }

    public bool IsAlive() =>
        Source.BindableObjectWeakReference.TryGetTarget(out _)
        && Target.BindableObjectWeakReference.TryGetTarget(out _);

    /// <summary>
    /// 断开绑定
    /// </summary>
    public void BreakBinding()
    {
        if (Source.BindableObjectWeakReference.TryGetTarget(out var sourceObject))
        {
            if (sourceObject is INotifyPropertyChanged sourceNotifyPropertyChanged)
            {
                sourceNotifyPropertyChanged.PropertyChanged -= Source_OnPropertyChanged;
            }
        }

        if (Target.BindableObjectWeakReference.TryGetTarget(out var targetObject))
        {
            if (targetObject is INotifyPropertyChanged targetNotifyPropertyChanged)
            {
                targetNotifyPropertyChanged.PropertyChanged -= Target_OnPropertyChanged;
            }
        }
    }
}
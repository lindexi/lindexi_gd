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

    #region Foreground

    public ImmutableBrush Foreground
    {
        set
        {
            _foreground = value;
        }
        get
        {
            return _foreground ?? StyleRunProperty?.Foreground ?? DefaultForeground;
        }
    }

    public static readonly ImmutableBrush DefaultForeground = new ImmutableBrush(Brushes.Black);

    private ImmutableBrush? _foreground;

    #endregion

    #region Background

    public ImmutableBrush? Background
    {
        set => _background = value;
        get => _background ?? StyleRunProperty?.Background ?? DefaultBackground;
    }

    public static ImmutableBrush? DefaultBackground => null;

    private ImmutableBrush? _background;

    #endregion



    private RunPropertyPlatformManager RunPropertyPlatformManager { get; }

    /// <summary>
    /// 继承样式里的属性
    /// </summary>
    private RunProperty? StyleRunProperty { get; }
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
using System;
using System.Windows;
using System.Windows.Media;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus;

/// <summary>
/// 光标的配置
/// </summary>
[APIConstraint("CaretConfiguration.txt")]
public class CaretConfiguration
{
    /// <summary>
    /// 光标的宽度
    /// </summary>
    public double CaretWidth { get; set; } = DefaultCaretWidth;

    /// <summary>
    /// 默认的光标宽度
    /// </summary>
    public const double DefaultCaretWidth = 2;

    /// <summary>
    /// 光标闪烁的时间
    /// </summary>
    public TimeSpan CaretBlinkTime
    {
        set
        {
            _caretBlinkTime = value;
        }
        get
        {
            if (_caretBlinkTime is null)
            {
                var caretBlinkTime = Win32.User32.GetCaretBlinkTime();
                // 要求闪烁至少是16毫秒。因为可能拿到 0 的值
                caretBlinkTime = Math.Max(16, caretBlinkTime);
                caretBlinkTime = Math.Min(1000, caretBlinkTime);
                _caretBlinkTime = TimeSpan.FromMilliseconds(caretBlinkTime);
            }

            return _caretBlinkTime.Value;
        }
    }

    private TimeSpan? _caretBlinkTime;

    /// <summary>
    /// 光标颜色，如是空，将采用字符属性前景色
    /// </summary>
    public Brush? CaretBrush
    {
        set;
        get;
    }

    /// <summary>
    /// 不在编辑状态时，保留显示选择范围
    /// </summary>
    /// 默认 WPF 的 TextBox 是不保留显示的
    public bool ShowSelectionWhenNotInEditingInputMode { set; get; } = true;

    /// <summary>
    /// 选择的画刷，默认是 SystemColors.HighlightColor 颜色
    /// </summary>
    public Brush SelectionBrush
    {
        set => _selectionBrush = value;
        get
        {
            if (_selectionBrush is null)
            {
                var brush = new SolidColorBrush(SystemColors.HighlightColor) { Opacity = 0.5 };
                brush.Freeze();
                _selectionBrush = brush;
            }
            
            return _selectionBrush;
        }
    }

    private Brush? _selectionBrush;
}

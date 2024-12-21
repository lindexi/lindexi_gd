using System;

using SkiaSharp;

namespace LightTextEditorPlus;

/// <summary>
/// 光标的配置
/// </summary>
[APIConstraint("CaretConfiguration.txt")]
public class SkiaCaretConfiguration
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
            //if (_caretBlinkTime is null)
            //{
            //    var caretBlinkTime = Win32.User32.GetCaretBlinkTime();
            //    // 要求闪烁至少是16毫秒。因为可能拿到 0 的值
            //    caretBlinkTime = Math.Max(16, caretBlinkTime);
            //    caretBlinkTime = Math.Min(1000, caretBlinkTime);
            //    _caretBlinkTime = TimeSpan.FromMilliseconds(caretBlinkTime);
            //}

            return _caretBlinkTime ?? TimeSpan.FromMilliseconds(16);
        }
    }

    private TimeSpan? _caretBlinkTime;

    public SKColor? CaretBrush { get; set; } = SKColors.Black;

    /// <summary>
    /// 不在编辑状态时，保留显示选择范围
    /// </summary>
    /// 默认 WPF 的 TextBox 是不保留显示的
    public bool ShowSelectionWhenNotInEditingInputMode { set; get; } = true;

    public SKColor SelectionBrush { get; set; } = SKColors.Blue.WithAlpha(0x50);
}

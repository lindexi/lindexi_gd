using System;

namespace LightTextEditorPlus;

/// <summary>
/// 光标的配置
/// </summary>
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
}
using System;
using System.Windows.Media;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus;

public class CaretConfiguration
{
    /// <summary>
    /// 光标的宽度
    /// </summary>
    public double CaretWidth { get; set; } = DefaultCaretWidth;

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
}
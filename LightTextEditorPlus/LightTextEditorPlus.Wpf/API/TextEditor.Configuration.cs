using System;

using System.Windows;
using LightTextEditorPlus.Rendering;

namespace LightTextEditorPlus;

// 此文件存放配置相关的方法
[APIConstraint("TextEditor.Configuration.txt")]
partial class TextEditor
{
    /// <summary>
    /// 文本库的静态配置
    /// </summary>
    public static StaticConfiguration StaticConfiguration { get; } = new StaticConfiguration();

    /// <summary>
    /// 光标的配置
    /// </summary>
    public CaretConfiguration CaretConfiguration { get; set; } = new CaretConfiguration();

    /// <summary>
    /// 文本的光标样式。由于 <see cref="FrameworkElement.Cursor"/> 属性将会被此类型赋值，导致如果想要定制光标，将会被覆盖
    /// </summary>
    public CursorStyles? CursorStyles
    {
        set
        {
            _cursorStyles = value;
            CursorStylesChanged?.Invoke(this,EventArgs.Empty);
        }
        get => _cursorStyles;
    }

    private CursorStyles? _cursorStyles;
    internal event EventHandler? CursorStylesChanged;
}

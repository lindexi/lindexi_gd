using System;

#if USE_WPF
using System.Windows;
using LightTextEditorPlus.Rendering;
#elif USE_SKIA
using CaretConfiguration = LightTextEditorPlus.SkiaCaretConfiguration;
#endif

namespace LightTextEditorPlus;

// 此文件存放配置相关的方法
partial class
#if USE_SKIA
    SkiaTextEditor
#else
    TextEditor
#endif
{
    /// <summary>
    /// 光标显示的配置
    /// </summary>
#if USE_WPF || USE_SKIA
    public CaretConfiguration CaretConfiguration { get; set; } = new CaretConfiguration();
#elif USE_AVALONIA
    public CaretConfiguration CaretConfiguration
    {
        get => new CaretConfiguration(SkiaTextEditor.CaretConfiguration);
        set => SkiaTextEditor.CaretConfiguration = value;
    }
#endif


#if USE_WPF
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
#endif
}
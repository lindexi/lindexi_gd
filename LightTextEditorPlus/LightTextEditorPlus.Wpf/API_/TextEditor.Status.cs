using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus;

// 此文件存放状态获取相关的方法

#if USE_SKIA
public partial class SkiaTextEditor
#else
public partial class TextEditor
#endif
{
#if USE_WPF
    /// <summary>
    /// 当前光标下的文本字符属性
    /// </summary>
    public IRunProperty CurrentCaretRunProperty => (IRunProperty)TextEditorCore.DocumentManager.CurrentCaretRunProperty;
#endif

    #region 光标和选择

    /// <summary>
    /// 获取或设置当前光标位置
    /// </summary>
    [TextEditorPublicAPI]
    public CaretOffset CurrentCaretOffset
    {
        set => TextEditorCore.CurrentCaretOffset = value;
        get => TextEditorCore.CurrentCaretOffset;
    }

    /// <summary>
    /// 获取或设置当前的选择范围
    /// </summary>
    [TextEditorPublicAPI]
    public Selection CurrentSelection
    {
        set => TextEditorCore.CurrentSelection = value;
        get => TextEditorCore.CurrentSelection;
    }

    #endregion
}
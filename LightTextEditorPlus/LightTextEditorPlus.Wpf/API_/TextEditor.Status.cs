using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus;

// 此文件存放状态获取相关的方法
public partial class TextEditor
{
    #region 光标和选择

    /// <summary>
    /// 获取或设置当前光标位置
    /// </summary>
    public CaretOffset CurrentCaretOffset
    {
        set => TextEditorCore.CurrentCaretOffset = value;
        get => TextEditorCore.CurrentCaretOffset;
    }

    /// <summary>
    /// 获取或设置当前的选择范围
    /// </summary>
    public Selection CurrentSelection
    {
        set => TextEditorCore.CurrentSelection = value;
        get => TextEditorCore.CurrentSelection;
    }

    #endregion
}
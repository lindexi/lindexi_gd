using System;
using LightTextEditorPlus.Core.Document.DocumentManagers;
using LightTextEditorPlus.Core.Events;

namespace LightTextEditorPlus.Core.Carets;

/// <summary>
/// 光标管理
/// </summary>
class CaretManager
{
    public CaretManager(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    private TextEditorCore TextEditor { get; }
    private DocumentManager DocumentManager => TextEditor.DocumentManager;

    #region 事件

    internal event EventHandler<TextEditorValueChangeEventArgs<CaretOffset>>?
        InternalCurrentCaretOffsetChanging;

    internal event EventHandler<TextEditorValueChangeEventArgs<CaretOffset>>?
        InternalCurrentCaretOffsetChanged;

    internal event EventHandler<TextEditorValueChangeEventArgs<Selection>>? InternalCurrentSelectionChanging;

    internal event EventHandler<TextEditorValueChangeEventArgs<Selection>>? InternalCurrentSelectionChanged;

    #endregion

    /// <summary>
    /// 获取或设置当前光标位置
    /// </summary>
    public CaretOffset CurrentCaretOffset
    {
        set
        {
            var oldValue = _currentCaretOffset;
            _isCurrentCaretOffsetChanging = true;

            // todo 完成光标系统
            // todo 设置光标的选择范围
            var args = new TextEditorValueChangeEventArgs<CaretOffset>(oldValue, value);
            InternalCurrentCaretOffsetChanging?.Invoke(this, args);

            // todo 处理越界
            _currentCaretOffset = value;

            // 如果当前的进入不是由选择范围触发的，那么更新选择范围
            if (_isCurrentSelectionChanging is false)
            {
                CurrentSelection = new Selection(value, 0);
            }

            _isCurrentCaretOffsetChanging = false;
            InternalCurrentCaretOffsetChanged?.Invoke(this, args);
        }
        get
        {
            return _currentCaretOffset;
        }
    }

    private CaretOffset _currentCaretOffset = new CaretOffset(0);

    /// <summary>
    /// 设置选择范围
    /// </summary>
    /// <param name="selection"></param>
    public void SetSelection(in Selection selection)
    {
        CurrentSelection = selection;
    }

    /// <summary>
    /// 获取或设置当前的选择范围
    /// </summary>
    /// 当没有选择时，将和 <see cref="CurrentCaretOffset"/> 相同
    public Selection CurrentSelection
    {
        // 设置为私有，只允许内部设置。方便打断点，解决 SetSelection 接收对外调用
        private set
        {
            var oldValue = _currentSelection;

            var args = new TextEditorValueChangeEventArgs<Selection>(oldValue, value);
            _isCurrentSelectionChanging = true;
            InternalCurrentSelectionChanging?.Invoke(this, args);

            _currentSelection = value;

            // 如果当前的进入不是由光标触发的，那么更新光标
            if (_isCurrentCaretOffsetChanging is false)
            {
                CurrentCaretOffset = value.EndOffset;
            }
            // todo 处理越界
            _isCurrentSelectionChanging = false;
            InternalCurrentSelectionChanged?.Invoke(this, args);
        }
        get
        {
            return _currentSelection;
        }
    }

    private Selection _currentSelection = new Selection(new CaretOffset(0), 0);

    private bool _isCurrentSelectionChanging;
    private bool _isCurrentCaretOffsetChanging;
}
using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Editing;
using LightTextEditorPlus.Core.Events;

namespace LightTextEditorPlus.Core.Carets;

/// <summary>
/// 光标管理
/// </summary>
/// todo 光标系统
/// - 项目符号
///   - 在空段里面，如果设置项目符号，在光标落在此空段内，将会显示半透明的项目符号。可以提供开关，给底层进行设置，或者是只是触发一个平台渲染层，因为没有影响布局
/// - 光标落在 \r\n 的中间的处理
/// - 光标变更的时候，可视化的光标是否跟随文本字符属性的颜色和字号，以及横排和竖排，以及光标所在的坐标，这是需要通知到渲染层
class CaretManager
{
    public CaretManager(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    /// <summary>
    /// 当前光标的字符属性
    /// </summary>
    /// 用户可以设置当前光标的字符属性，但是在光标切走之后，将会自动清掉此属性
    /// 之前我做的文本库的设计是当前光标的字符属性和 <see cref="P:LightTextEditorPlus.Core.Document.DocumentManager.CurrentRunProperty"/> 耦合，存在的一个问题是文档的字符属性不断被变更
    /// 即使点击到段首，设置字符属性也不会影响当前段落字符属性。当前段落字符属性仅仅受到首个字符的影响
    public IReadOnlyRunProperty? CurrentCaretRunProperty { get; set; }

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
            if ((_currentCaretOffset.Offset == value.Offset && _currentCaretOffset.IsAtLineStart == value.IsAtLineStart)
                // 如果在选择下修改了光标，那就需要执行后续步骤，用来清理选择
                // 因此只有在无选择的情况下，如果光标未变更，才啥都不执行
                && CurrentSelection.IsEmpty)
            {
                return;
            }

            var oldValue = _currentCaretOffset;
            _isCurrentCaretOffsetChanging = true;

            // todo 完成光标系统
            var args = new TextEditorValueChangeEventArgs<CaretOffset>(oldValue, value);
            InternalCurrentCaretOffsetChanging?.Invoke(this, args);

            // 清理当前光标的属性
            CurrentCaretRunProperty = null;

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
        get { return _currentCaretOffset; }
    }

    private CaretOffset _currentCaretOffset = new CaretOffset(0);

    /// <summary>
    /// 设置选择范围
    /// </summary>
    /// <param name="selection"></param>
    public void SetSelection(in Selection selection)
    {
        if (TextEditor.CheckFeaturesDisableWithLog(TextFeatures.SelectionEnable))
        {
            return;
        }

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
        get { return _currentSelection; }
    }

    private Selection _currentSelection = new Selection(new CaretOffset(0), 0);

    private bool _isCurrentSelectionChanging;
    private bool _isCurrentCaretOffsetChanging;
}

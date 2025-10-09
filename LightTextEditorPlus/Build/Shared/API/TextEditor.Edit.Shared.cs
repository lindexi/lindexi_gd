#if USE_AllInOne || !USE_MauiGraphics && !USE_SKIA

using System;
using System.Text;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Editing;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Editing;

namespace LightTextEditorPlus
{
    // 这里存放多个平台的共享代码
    [APIConstraint("TextEditor.Edit.Shared.txt")]
    partial class TextEditor
    {
        #region Text

        /// <summary>
        /// 获取当前选中的文本
        /// </summary>
        /// <returns></returns>
        [TextEditorPublicAPI]
        public string GetSelectedText()
        {
            return TextEditorCore.GetText(TextEditorCore.CurrentSelection);
        }

        /// <summary>
        /// 获取文本
        /// </summary>
        /// <remarks>如需获取富文本，请使用 <see cref="GetRunList(in Selection)"/> 方法</remarks>
        /// <returns></returns>
        [TextEditorPublicAPI]
        public string GetText(in Selection selection)
        {
            return TextEditorCore.GetText(in selection);
        }

        /// <summary>
        /// 获取文本
        /// </summary>
        /// <remarks>如需获取富文本，请使用 <see cref="GetRunList(in Selection)"/> 方法</remarks>
        /// <param name="selection"></param>
        /// <param name="stringBuilder"></param>
        /// <returns></returns>
        [TextEditorPublicAPI]
        public StringBuilder GetText(in Selection selection, StringBuilder stringBuilder)
        {
            return TextEditorCore.GetText(stringBuilder, in selection);
        }

        #endregion

        #region 编辑模式

        /// <summary>
        /// 是否进入用户编辑模式。进入用户编辑模式将闪烁光标，支持输入法输入
        /// </summary>
        public bool IsInEditingInputMode
        {
            set
            {
                if (field == value)
                {
                    return;
                }

                if (value is true && IsEditable is false)
                {
                    Logger.LogDebug("设置进入用户编辑模式，但当前文本禁用编辑，设置进入用户编辑模式失效");

                    value = false;
                }

                if (value)
                {
                    EnsureEditInit();
                }

                Logger.LogDebug(value ? "进入用户编辑模式" : "退出用户编辑模式");

                field = value;

                if (value)
                {
                    Focus();
                    EnterEditingCursor();
                }
                else
                {
                    LeaveEditingCursor();
                }

                IsInEditingInputModeChanged?.Invoke(this, EventArgs.Empty);

                // 让光标有得刷新
                InvalidateVisual();
            }
            get;
        } = false;

        /// <summary>
        /// 是否进入编辑的模式变更完成事件
        /// </summary>
        public event EventHandler? IsInEditingInputModeChanged;

        /// <summary>
        /// 是否自动根据是否获取焦点设置是否进入编辑模式
        /// </summary>
        public bool IsAutoEditingModeByFocus
        {
            get => field && IsEditable;
            set
            {
                if (!IsEditable && value)
                {
                    Logger.LogWarning($"由于当前文本禁用编辑，设置自动编辑模式失效 Set IsAutoEditingModeByFocus Fail. IsEditable=False");
                }
                field = value;
            }
        } = true;

        /// <summary>
        /// 确保编辑功能初始化完成
        /// </summary>
        private void EnsureEditInit()
        {
            if (_isInitEdit) return;
            _isInitEdit = true;

            if (_keyboardHandler == null)
            {
                _keyboardHandler = new KeyboardHandler(this);
                KeyboardBindingInitializedInner?.Invoke(this, EventArgs.Empty);
                // 一次性的事件，触发之后就释放掉
                KeyboardBindingInitializedInner = null;
            }
        }

        private bool _isInitEdit;

        private KeyboardHandler? _keyboardHandler;

        /// <summary>
        /// 键盘绑定已经初始化完成事件
        /// </summary>
        /// 可在此事件之后，改写键盘绑定快捷键
        public event EventHandler? KeyboardBindingInitialized
        {
            add
            {
                if (_keyboardHandler is not null)
                {
                    value?.Invoke(this, EventArgs.Empty);
                    return;
                }

                KeyboardBindingInitializedInner += value;
            }
            remove => KeyboardBindingInitializedInner -= value;
        }

        private event EventHandler? KeyboardBindingInitializedInner;

        private partial void EnterEditingCursor();
        private partial void LeaveEditingCursor();

        /// <summary>
        /// 是否可编辑。可编辑 <see cref="IsEditable"/> 和 <see cref="IsInEditingInputMode"/> 不同点在于，可编辑 <see cref="IsEditable"/>  是指是否开放用户编辑，不可编辑时用户无法编辑文本。而 <see cref="IsInEditingInputMode"/> 指的是当前的状态是否是用户编辑状态
        /// <br/>
        /// 即 可编辑 <see cref="IsEditable"/> 决定能否进入编辑状态，而 <see cref="IsInEditingInputMode"/> 表示现在是否处于编辑状态
        /// <br/>
        /// 可以认为 可编辑 <see cref="IsEditable"/> 为 false 时，就是 <see cref="IsReadOnly"/> 只读模式
        /// </summary>
        /// <remarks>
        /// 设置不可编辑时，仅仅是不开放用户编辑，但是依然可以通过 API 进行编辑修改文本内容。如果需要完全禁止编辑，请使用 <see cref="TextFeatures"/> 功能开关进行业务端禁用
        /// </remarks>
        public bool IsEditable
        {
            get;
            set
            {
                if (value == field)
                {
                    return;
                }
                var oldValue = field;

                field = value;
                IsInEditingInputMode = false;

                IsEditableChanged?.Invoke(this, new TextEditorValueChangeEventArgs<bool>(oldValue, value));
            }
        } = true;

        /// <summary>
        /// 是否可编辑变更事件
        /// </summary>
        public event EventHandler<TextEditorValueChangeEventArgs<bool>>? IsEditableChanged;

        /// <summary>
        /// 是否只读的文本。这里的只读指的是不开放用户编辑，依然可以使用 API 调用进行文本编辑。如需进入或退出只读模式，请设置 <see cref="IsEditable"/> 属性
        /// </summary>
        public bool IsReadOnly => !IsEditable;

        /// <summary>
        /// 进入编辑模式
        /// </summary>
        [TextEditorPublicAPI]
        public void EnterEditMode()
        {
            if (!IsEditable)
            {
                throw new InvalidOperationException($"当前文本不可编辑 IsEditable=false 不能进入编辑模式");
            }

            IsInEditingInputMode = true;
        }

        /// <summary>
        /// 退出编辑模式
        /// </summary>
        [TextEditorPublicAPI]
        public void QuitEditMode()
        {
            IsInEditingInputMode = false;
            TextEditorCore.ClearSelection();
        }

        /// <summary>
        /// 是否处于覆盖模式
        /// </summary>
        /// 是否处于替换模式
        /// 
        /// 覆盖模式： 按下 Insert 键，光标会变成下划横线，输入的字符会替换光标所在位置后面的字符
        public bool IsOvertypeMode
        {
            get;
            set
            {
                if (value && TextEditorCore.CheckFeaturesDisableWithLog(TextFeatures.OvertypeModeEnable))
                {
                    return;
                }

                if (value == field)
                {
                    return;
                }

                var oldValue = field;
                field = value;

                if (value)
                {
                    Logger.LogDebug("EnterOvertypeMode");
                }
                else
                {
                    Logger.LogDebug("QuitOvertypeMode");
                }
                
                IsOvertypeModeChanged?.Invoke(this, new TextEditorValueChangeEventArgs<bool>(oldValue, value));

                InvalidateVisual();
            }
        }

        /// <summary>
        /// 是否处于覆盖模式变更事件
        /// </summary>
        public event EventHandler<TextEditorValueChangeEventArgs<bool>>? IsOvertypeModeChanged;

        #endregion

        #region 段落属性

        /// <inheritdoc cref="TextEditorCore.ParagraphList"/>
        public TextEditorParagraphList ParagraphList =>
            field ??= new TextEditorParagraphList(TextEditorCore.ParagraphList);

        /// <summary>
        /// 设置段落属性
        /// </summary>
        /// <param name="caretOffset"></param>
        /// <param name="paragraphProperty"></param>
        public void SetParagraphProperty(in CaretOffset caretOffset, ParagraphProperty paragraphProperty)
        {
            TextEditorCore.DocumentManager.SetParagraphProperty(caretOffset, paragraphProperty);
        }

        /// <summary>
        /// 设置段落属性
        /// </summary>
        public void SetParagraphProperty(ParagraphIndex index, ParagraphProperty paragraphProperty)
        {
            TextEditorCore.DocumentManager.SetParagraphProperty(index, paragraphProperty);
        }

        /// <summary>
        /// 配置更改段落属性。此方法等同于手动调用 <see cref="GetParagraphProperty(in CaretOffset)"/> 获取段落属性，再调用 <see cref="SetParagraphProperty(in LightTextEditorPlus.Core.Carets.CaretOffset,LightTextEditorPlus.Core.Document.ParagraphProperty)"/> 设置段落属性
        /// </summary>
        /// <param name="caretOffset"></param>
        /// <param name="config">传入的样式段落属性为当前准备更改的段落的段落属性</param>
        public void ConfigParagraphProperty(in CaretOffset caretOffset, CreateParagraphPropertyDelegate config)
        {
            ParagraphProperty paragraphProperty = GetParagraphProperty(caretOffset);
            ParagraphProperty newParagraphProperty = config(paragraphProperty);
            SetParagraphProperty(in caretOffset, newParagraphProperty);
        }

        /// <summary>
        /// 配置更改段落属性。此方法等同于手动调用 <see cref="GetParagraphProperty(ParagraphIndex)"/> 获取段落属性，再调用 <see cref="SetParagraphProperty(ParagraphIndex,LightTextEditorPlus.Core.Document.ParagraphProperty)"/> 设置段落属性
        /// </summary>
        /// <param name="index"></param>
        /// <param name="config">传入的样式段落属性为当前准备更改的段落的段落属性</param>
        public void ConfigParagraphProperty(ParagraphIndex index, CreateParagraphPropertyDelegate config)
        {
            ParagraphProperty paragraphProperty = GetParagraphProperty(index);
            ParagraphProperty newParagraphProperty = config(paragraphProperty);
            SetParagraphProperty(index, newParagraphProperty);
        }

        /// <summary>
        /// 设置当前光标所在的段落的段落属性
        /// </summary>
        /// <param name="paragraphProperty"></param>
        public void SetCurrentCaretOffsetParagraphProperty(ParagraphProperty paragraphProperty) => SetParagraphProperty(TextEditorCore.CurrentCaretOffset, paragraphProperty);

        /// <summary>
        /// 配置当前光标所在的段落的段落属性
        /// </summary>
        /// <param name="config"></param>
        public void ConfigCurrentCaretOffsetParagraphProperty(CreateParagraphPropertyDelegate config)
        {
            CaretOffset currentCaretOffset = TextEditorCore.CurrentCaretOffset;
            ParagraphProperty paragraphProperty = GetParagraphProperty(currentCaretOffset);
            SetParagraphProperty(currentCaretOffset, config(paragraphProperty));
        }

        /// <summary>
        /// 获取段落属性
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ParagraphProperty GetParagraphProperty(ParagraphIndex index)
        {
            return TextEditorCore.DocumentManager.GetParagraphProperty(index);
        }

        /// <summary>
        /// 获取段落属性
        /// </summary>
        /// <param name="caretOffset"></param>
        /// <returns></returns>
        public ParagraphProperty GetParagraphProperty(in CaretOffset caretOffset)
        {
            return TextEditorCore.DocumentManager.GetParagraphProperty(in caretOffset);
        }

        /// <summary>
        /// 获取当前光标所在的段落的段落属性
        /// </summary>
        /// <returns></returns>
        public ParagraphProperty GetCurrentCaretOffsetParagraphProperty()
            => GetParagraphProperty(TextEditorCore.CurrentCaretOffset);

        /// <inheritdoc cref="DocumentManager.StyleParagraphProperty"/>
        public ParagraphProperty StyleParagraphProperty => TextEditorCore.DocumentManager.StyleParagraphProperty;

        /// <inheritdoc cref="DocumentManager.SetStyleParagraphProperty"/>
        public void SetStyleParagraphProperty(ParagraphProperty paragraphProperty)
        {
            TextEditorCore.DocumentManager.SetStyleParagraphProperty(paragraphProperty);
        }

        #endregion

        /// <summary>
        /// 设置或获取文本编辑器的交互处理器
        /// </summary>
        public TextEditorHandler TextEditorHandler
        {
            get => field ??= TextEditorPlatformProvider.GetHandler();
            set;
        }
    }
}
#endif

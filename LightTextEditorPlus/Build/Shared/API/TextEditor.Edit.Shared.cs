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
        /// 是否进入用户编辑模式。进入用户编辑模式将闪烁光标，支持输入法输入。只影响用户的交互输入，不影响通过 API 进行文本编辑
        /// </summary>
        public bool IsInEditingInputMode
        {
            set
            {
                // 这个属性还负担着设置光标的功能，即使属性值没有变化，也需要执行后续逻辑
                //if (_isInEditingInputMode == value)
                //{
                //    return;
                //}

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

                _isInEditingInputMode = value;

                if (value)
                {
                    Focus();
                    EnterEditingCursor();
                }
                else
                {
                    if (CaretConfiguration.ShowCaretAndSelectionInReadonlyMode)
                    {
                        EnterEditingCursor();
                    }
                    else
                    {
                        LeaveEditingCursor();
                    }
                }

                IsInEditingInputModeChanged?.Invoke(this, EventArgs.Empty);

                // 让光标有得刷新
                InvalidateVisual();
            }
            get => _isInEditingInputMode;
        }

        private bool _isInEditingInputMode = false;

        /// <summary>
        /// 是否进入编辑的模式变更完成事件
        /// </summary>
        public event EventHandler? IsInEditingInputModeChanged;

        /// <summary>
        /// 是否自动根据是否获取焦点设置是否进入编辑模式
        /// </summary>
        public bool IsAutoEditingModeByFocus
        {
            get => _isAutoEditingModeByFocus && IsEditable;
            set
            {
                if (!IsEditable && value)
                {
                    Logger.LogWarning($"由于当前文本禁用编辑，设置自动编辑模式失效 Set IsAutoEditingModeByFocus Fail. IsEditable=False");
                }

                _isAutoEditingModeByFocus = value;
            }
        }

        private bool _isAutoEditingModeByFocus = true;

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
            get => _isEditable;
            set
            {
                if (value == _isEditable)
                {
                    return;
                }

                var oldValue = _isEditable;

                _isEditable = value;
                IsInEditingInputMode = false;

                IsEditableChanged?.Invoke(this, new TextEditorValueChangeEventArgs<bool>(oldValue, value));
            }
        }

        private bool _isEditable = true;

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
            get => _isOvertypeMode;
            set
            {
                if (value && TextEditorCore.CheckFeaturesDisableWithLog(TextFeatures.OvertypeModeEnable))
                {
                    return;
                }

                if (value == _isOvertypeMode)
                {
                    return;
                }

                var oldValue = _isOvertypeMode;
                _isOvertypeMode = value;

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

        private bool _isOvertypeMode;

        /// <summary>
        /// 是否处于覆盖模式变更事件
        /// </summary>
        public event EventHandler<TextEditorValueChangeEventArgs<bool>>? IsOvertypeModeChanged;

        #endregion

        #region Style

        /// <summary>
        /// 增加字体大小
        /// </summary>
        public void IncreaseFontSize(Selection? selection = null)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.IncreaseFontSize)
                || CheckFeaturesDisableWithLog(TextFeatures.SetFontSize))
            {
                return;
            }

            SetRunProperty(p => p with { FontSize = p.FontSize + 1 }, PropertyType.FontSize, selection);
        }

        /// <summary>
        /// 减小字体大小
        /// </summary>
        public void DecreaseFontSize(Selection? selection = null)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.DecreaseFontSize)
                || CheckFeaturesDisableWithLog(TextFeatures.SetFontSize))
            {
                return;
            }

            SetRunProperty(p => p with { FontSize = p.FontSize - 1 }, PropertyType.FontSize, selection);
        }

        #endregion

        #region 段落属性

        /// <inheritdoc cref="TextEditorCore.ParagraphList"/>
        public TextEditorParagraphList ParagraphList =>
            _paragraphList ??= new TextEditorParagraphList(TextEditorCore.ParagraphList);

        private TextEditorParagraphList? _paragraphList;

        /// <summary>
        /// 设置段落属性
        /// </summary>
        /// <param name="caretOffset">光标位置</param>
        /// <param name="paragraphProperty"></param>
        public void SetParagraphProperty(in CaretOffset caretOffset, ParagraphProperty paragraphProperty)
        {
            TextEditorCore.DocumentManager.SetParagraphProperty(caretOffset, paragraphProperty);
        }

        /// <summary>
        /// 设置段落属性
        /// </summary>
        /// <param name="index">段落序号</param>
        /// <param name="paragraphProperty"></param>
        public void SetParagraphProperty(ParagraphIndex index, ParagraphProperty paragraphProperty)
        {
            TextEditorCore.DocumentManager.SetParagraphProperty(index, paragraphProperty);
        }

        /// <summary>
        /// 配置更改段落属性。此方法等同于手动调用 <see cref="GetParagraphProperty(in CaretOffset)"/> 获取段落属性，再调用 <see cref="SetParagraphProperty(in LightTextEditorPlus.Core.Carets.CaretOffset,LightTextEditorPlus.Core.Document.ParagraphProperty)"/> 设置段落属性
        /// </summary>
        /// <param name="caretOffset">光标位置</param>
        /// <param name="config">传入的样式段落属性为当前准备更改的段落的段落属性</param>
        public void ConfigParagraphProperty(in CaretOffset caretOffset, CreateParagraphPropertyDelegate config)
        {
            ITextParagraph textParagraph = TextEditorCore.DocumentManager.GetParagraph(in caretOffset);
            ParagraphProperty paragraphProperty = textParagraph.ParagraphProperty;
            ParagraphProperty newParagraphProperty = config(paragraphProperty);
            TextEditorCore.DocumentManager.SetParagraphProperty(textParagraph, newParagraphProperty);
        }

        /// <summary>
        /// 配置更改段落属性。此方法等同于手动调用 <see cref="GetParagraphProperty(ParagraphIndex)"/> 获取段落属性，再调用 <see cref="SetParagraphProperty(ParagraphIndex,LightTextEditorPlus.Core.Document.ParagraphProperty)"/> 设置段落属性
        /// </summary>
        /// <param name="index">段落序号</param>
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
        public void SetCurrentCaretOffsetParagraphProperty
            (ParagraphProperty paragraphProperty) =>
            SetParagraphProperty(TextEditorCore.CurrentCaretOffset, paragraphProperty);

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
        /// 增加当前段落的缩进
        /// </summary>
        /// <param name="value">缩进增量，默认值为 1</param>
        public void IncreaseIndentation(double value = 1)
            => IncreaseIndentation(TextEditorCore.CurrentCaretOffset, value);

        /// <summary>
        /// 增加指定光标所在段落的缩进
        /// </summary>
        /// <param name="caretOffset">光标位置</param>
        /// <param name="value">缩进增量，默认值为 1</param>
        public void IncreaseIndentation(in CaretOffset caretOffset, double value = 1)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.IncreaseIndentation)
                || CheckFeaturesDisableWithLog(TextFeatures.SetIndentation))
            {
                return;
            }

            ConfigParagraphProperty(caretOffset, paragraph => paragraph with { Indent = paragraph.Indent + value });
        }

        /// <summary>
        /// 增加指定段落的缩进
        /// </summary>
        /// <param name="index">段落序号</param>
        /// <param name="value">缩进增量，默认值为 1</param>
        public void IncreaseIndentation(ParagraphIndex index, double value = 1)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.IncreaseIndentation)
                || CheckFeaturesDisableWithLog(TextFeatures.SetIndentation))
            {
                return;
            }

            ConfigParagraphProperty(index, paragraph => paragraph with { Indent = paragraph.Indent + value });
        }

        /// <summary>
        /// 减少当前段落的缩进
        /// </summary>
        /// <param name="value">缩进减量，默认值为 1</param>
        public void DecreaseIndentation(double value = 1)
            => DecreaseIndentation(TextEditorCore.CurrentCaretOffset, value);

        /// <summary>
        /// 减少指定光标所在段落的缩进
        /// </summary>
        /// <param name="caretOffset">光标位置</param>
        /// <param name="value">缩进减量，默认值为 1</param>
        public void DecreaseIndentation(in CaretOffset caretOffset, double value = 1)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.DecreaseIndentation)
                || CheckFeaturesDisableWithLog(TextFeatures.SetIndentation))
            {
                return;
            }

            ConfigParagraphProperty(caretOffset, paragraph => paragraph with { Indent = paragraph.Indent - value });
        }

        /// <summary>
        /// 减少指定段落的缩进
        /// </summary>
        /// <param name="index">段落序号</param>
        /// <param name="value">缩进减量，默认值为 1</param>
        public void DecreaseIndentation(ParagraphIndex index, double value = 1)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.DecreaseIndentation)
                || CheckFeaturesDisableWithLog(TextFeatures.SetIndentation))
            {
                return;
            }

            ConfigParagraphProperty(index, paragraph => paragraph with { Indent = paragraph.Indent - value });
        }

        /// <summary>
        /// 设置当前段落缩进
        /// </summary>
        /// <param name="indent">缩进值</param>
        public void SetIndentation(double indent)
            => SetIndentation(TextEditorCore.CurrentCaretOffset, indent);

        /// <summary>
        /// 设置指定光标所在段落缩进
        /// </summary>
        /// <param name="caretOffset">光标位置</param>
        /// <param name="indent">缩进值</param>
        public void SetIndentation(in CaretOffset caretOffset, double indent)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.SetIndentation))
            {
                return;
            }

            ConfigParagraphProperty(caretOffset, paragraph => paragraph with { Indent = indent });
        }

        /// <summary>
        /// 设置指定段落缩进
        /// </summary>
        /// <param name="index">段落序号</param>
        /// <param name="indent">缩进值</param>
        public void SetIndentation(ParagraphIndex index, double indent)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.SetIndentation))
            {
                return;
            }

            ConfigParagraphProperty(index, paragraph => paragraph with { Indent = indent });
        }

        /// <summary>
        /// 设置当前段落段前间距
        /// </summary>
        /// <param name="paragraphBefore">段前间距</param>
        public void SetParagraphSpaceBefore(double paragraphBefore)
            => SetParagraphSpaceBefore(TextEditorCore.CurrentCaretOffset, paragraphBefore);

        /// <summary>
        /// 设置指定光标所在段落段前间距
        /// </summary>
        /// <param name="caretOffset">光标位置</param>
        /// <param name="paragraphBefore">段前间距</param>
        public void SetParagraphSpaceBefore(in CaretOffset caretOffset, double paragraphBefore)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.SetParagraphSpaceBefore))
            {
                return;
            }

            ConfigParagraphProperty(caretOffset, paragraph => paragraph with { ParagraphBefore = paragraphBefore });
        }

        /// <summary>
        /// 设置指定段落段前间距
        /// </summary>
        /// <param name="index">段落序号</param>
        /// <param name="paragraphBefore">段前间距</param>
        public void SetParagraphSpaceBefore(ParagraphIndex index, double paragraphBefore)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.SetParagraphSpaceBefore))
            {
                return;
            }

            ConfigParagraphProperty(index, paragraph => paragraph with { ParagraphBefore = paragraphBefore });
        }

        /// <summary>
        /// 设置当前段落段后间距
        /// </summary>
        /// <param name="paragraphAfter">段后间距</param>
        public void SetParagraphSpaceAfter(double paragraphAfter)
            => SetParagraphSpaceAfter(TextEditorCore.CurrentCaretOffset, paragraphAfter);

        /// <summary>
        /// 设置指定光标所在段落段后间距
        /// </summary>
        /// <param name="caretOffset">光标位置</param>
        /// <param name="paragraphAfter">段后间距</param>
        public void SetParagraphSpaceAfter(in CaretOffset caretOffset, double paragraphAfter)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.SetParagraphSpaceAfter))
            {
                return;
            }

            ConfigParagraphProperty(caretOffset, paragraph => paragraph with { ParagraphAfter = paragraphAfter });
        }

        /// <summary>
        /// 设置指定段落段后间距
        /// </summary>
        /// <param name="index">段落序号</param>
        /// <param name="paragraphAfter">段后间距</param>
        public void SetParagraphSpaceAfter(ParagraphIndex index, double paragraphAfter)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.SetParagraphSpaceAfter))
            {
                return;
            }

            ConfigParagraphProperty(index, paragraph => paragraph with { ParagraphAfter = paragraphAfter });
        }

        /// <summary>
        /// 设置当前段落行距
        /// </summary>
        /// <param name="lineSpacing">行距值</param>
        public void SetLineSpacing(ITextLineSpacing lineSpacing)
            => SetLineSpacing(TextEditorCore.CurrentCaretOffset, lineSpacing);

        /// <summary>
        /// 设置指定光标所在段落行距
        /// </summary>
        /// <param name="caretOffset">光标位置</param>
        /// <param name="lineSpacing">行距值</param>
        public void SetLineSpacing(in CaretOffset caretOffset, ITextLineSpacing lineSpacing)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.SetLineSpacing))
            {
                return;
            }

            ConfigParagraphProperty(caretOffset, paragraph => paragraph with { LineSpacing = lineSpacing });
        }

        /// <summary>
        /// 设置指定段落行距
        /// </summary>
        /// <param name="index">段落序号</param>
        /// <param name="lineSpacing">行距值</param>
        public void SetLineSpacing(ParagraphIndex index, ITextLineSpacing lineSpacing)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.SetLineSpacing))
            {
                return;
            }

            ConfigParagraphProperty(index, paragraph => paragraph with { LineSpacing = lineSpacing });
        }

        /// <summary>
        /// 设置当前段落水平对齐
        /// </summary>
        /// <param name="horizontalTextAlignment">水平对齐方式</param>
        public void SetHorizontalTextAlignment(HorizontalTextAlignment horizontalTextAlignment)
            => SetHorizontalTextAlignment(TextEditorCore.CurrentCaretOffset, horizontalTextAlignment);

        /// <summary>
        /// 设置指定光标所在段落水平对齐
        /// </summary>
        /// <param name="caretOffset">光标位置</param>
        /// <param name="horizontalTextAlignment">水平对齐方式</param>
        public void SetHorizontalTextAlignment(in CaretOffset caretOffset,
            HorizontalTextAlignment horizontalTextAlignment)
        {
            if (CheckHorizontalAlignFeatureDisableWithLog(horizontalTextAlignment))
            {
                return;
            }

            ConfigParagraphProperty(caretOffset,
                paragraph => paragraph with { HorizontalTextAlignment = horizontalTextAlignment });
        }

        /// <summary>
        /// 设置指定段落水平对齐
        /// </summary>
        /// <param name="index">段落序号</param>
        /// <param name="horizontalTextAlignment">水平对齐方式</param>
        public void SetHorizontalTextAlignment(ParagraphIndex index, HorizontalTextAlignment horizontalTextAlignment)
        {
            if (CheckHorizontalAlignFeatureDisableWithLog(horizontalTextAlignment))
            {
                return;
            }

            ConfigParagraphProperty(index,
                paragraph => paragraph with { HorizontalTextAlignment = horizontalTextAlignment });
        }

        private bool CheckHorizontalAlignFeatureDisableWithLog(HorizontalTextAlignment horizontalTextAlignment)
        {
            if (CheckFeaturesDisableWithLog(TextFeatures.HorizontalAlign))
            {
                return true;
            }

            return horizontalTextAlignment switch
            {
                HorizontalTextAlignment.Left => CheckFeaturesDisableWithLog(TextFeatures.AlignHorizontalLeft),
                HorizontalTextAlignment.Center => CheckFeaturesDisableWithLog(TextFeatures.AlignHorizontalCenter),
                HorizontalTextAlignment.Right => CheckFeaturesDisableWithLog(TextFeatures.AlignHorizontalRight),
                HorizontalTextAlignment.Justify => CheckFeaturesDisableWithLog(TextFeatures.AlignJustify),
                _ => false
            };
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

        #region 段落文本

        /// <inheritdoc cref="DocumentManager.GetParagraph(ParagraphIndex)"/>
        public ITextParagraph GetParagraph(ParagraphIndex paragraphIndex)
            => TextEditorCore.DocumentManager.GetParagraph(paragraphIndex);

        /// <inheritdoc cref="DocumentManager.GetParagraph(in CaretOffset)"/>
        public ITextParagraph GetParagraph(in CaretOffset caretOffset)
            => TextEditorCore.DocumentManager.GetParagraph(caretOffset);

        /// <summary>
        /// 获取当前光标下的段落
        /// </summary>
        /// <returns></returns>
        public ITextParagraph GetCurrentCaretOffsetParagraph()
            => TextEditorCore.DocumentManager.GetParagraph(TextEditorCore.CurrentCaretOffset);

        #endregion

        /// <summary>
        /// 设置或获取文本编辑器的交互处理器
        /// </summary>
        public TextEditorHandler TextEditorHandler
        {
            get => _textEditorHandler ??= CreateTextEditorHandler();
            set => _textEditorHandler = value;
        }

        private TextEditorHandler? _textEditorHandler;

        /// <summary>
        /// 创建文本编辑器的交互处理器
        /// </summary>
        /// <returns></returns>
        protected virtual TextEditorHandler CreateTextEditorHandler() => TextEditorPlatformProvider.GetHandler();
    }
}
#endif
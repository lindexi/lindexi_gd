#if USE_AllInOne || !USE_MauiGraphics && !USE_SKIA

using System;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Editing;
using LightTextEditorPlus.Core.Events;

namespace LightTextEditorPlus
{
    // 这里存放多个平台的共享代码
    [APIConstraint("TextEditor.Edit.Shared.txt")]
    partial class TextEditor
    {
        #region 编辑模式

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
                _isEditable = value;
                IsInEditingInputMode = false;

                IsEditableChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool _isEditable = true;

        /// <summary>
        /// 是否可编辑变更事件
        /// </summary>
        public event EventHandler? IsEditableChanged;

        /// <summary>
        /// 是否只读的文本。这里的只读指的是不开放用户编辑，依然可以使用 API 调用进行文本编辑。如需进入或退出只读模式，请设置 <see cref="IsEditable"/> 属性
        /// </summary>
        public bool IsReadOnly => !IsEditable;

        /// <summary>
        /// 进入编辑模式
        /// </summary>
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
                IsOvertypeModeChanged?.Invoke(this, new TextEditorValueChangeEventArgs<bool>(oldValue, value));
            }
        }

        private bool _isOvertypeMode;

        /// <summary>
        /// 是否处于覆盖模式变更事件
        /// </summary>
        public event EventHandler<TextEditorValueChangeEventArgs<bool>>? IsOvertypeModeChanged;

        #endregion

        #region 段落属性

        /// <summary>
        /// 设置段落属性
        /// </summary>
        /// <param name="caretOffset"></param>
        /// <param name="paragraphProperty"></param>
        public void SetParagraphProperty(CaretOffset caretOffset, ParagraphProperty paragraphProperty)
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
        /// 设置当前光标所在的段落的段落属性
        /// </summary>
        /// <param name="paragraphProperty"></param>
        public void SetCurrentCaretOffsetParagraphProperty(ParagraphProperty paragraphProperty) => SetParagraphProperty(TextEditorCore.CurrentCaretOffset, paragraphProperty);

        /// <summary>
        /// 配置当前光标所在的段落的段落属性
        /// </summary>
        /// <param name="config"></param>
        public void ConfigCurrentCaretOffsetParagraphProperty(Func<ParagraphProperty, ParagraphProperty> config)
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
        public ParagraphProperty GetParagraphProperty(CaretOffset caretOffset)
        {
            return TextEditorCore.DocumentManager.GetParagraphProperty(caretOffset);
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
    }
}
#endif

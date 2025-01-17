#if USE_AllInOne || !USE_MauiGraphics && !USE_SKIA

using System;
using System.Reflection;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus
{
    // 这里存放多个平台的共享代码
    [APIConstraint("TextEditor.Edit.Shared.txt")]
    partial class TextEditor
    {
        #region 编辑模式

        /// <summary>
        /// 是否可编辑。可编辑和 <see cref="IsInEditingInputMode"/> 不同点在于，可编辑是指是否开放用户编辑，不可编辑时用户无法编辑文本。而 <see cref="IsInEditingInputMode"/> 指的是当前的状态是否是用户编辑状态
        /// </summary>
        /// <remarks>
        /// 设置不可编辑时，仅仅是不开放用户编辑，但是可以通过 API 进行编辑
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

        #endregion
    }
}
#endif

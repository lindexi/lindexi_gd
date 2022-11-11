
using LightTextEditorPlus.Core.Document.Segments;

using System;
using System.Collections.Specialized;
using System.Linq;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Events;
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Document.DocumentManagers
{
    /// <summary>
    /// 提供文档管理，只提供数据管理，这里属于更高级的 API 层，将提供更多的细节控制
    /// </summary>
    public class DocumentManager
    {
        public DocumentManager(TextEditor textEditor)
        {
            TextEditor = textEditor;
            CurrentParagraphProperty = new ParagraphProperty();
            CurrentRunProperty = new RunProperty();

            TextRunManager = new TextRunManager(textEditor);
        }

        #region 框架
        public TextEditorCore TextEditor { get; }

        /// <summary>
        /// 文档的宽度
        /// </summary>
        internal double DocumentWidth { set; get; } = double.PositiveInfinity;

        /// <summary>
        /// 文档的高度
        /// </summary>
        internal double DocumentHeight { set; get; } = double.PositiveInfinity;

        internal TextRunManager TextRunManager { get; }

        #region 事件

        internal event EventHandler<TextEditorValueChangeEventArgs<CaretOffset>>?
            InternalCurrentCaretOffsetChanging;

        internal event EventHandler<TextEditorValueChangeEventArgs<CaretOffset>>?
            InternalCurrentCaretOffsetChanged;

        internal event EventHandler<TextEditorValueChangeEventArgs<Selection>>? InternalCurrentSelectionChanging;

        internal event EventHandler<TextEditorValueChangeEventArgs<Selection>>? InternalCurrentSelectionChanged;

        #endregion

        /// <summary>
        /// 给内部提供的文档开始变更事件
        /// </summary>
        internal event EventHandler? InternalDocumentChanging;

        /// <summary>
        /// 给内部提供的文档变更完成事件
        /// </summary>
        internal event EventHandler? InternalDocumentChanged;

        /// <summary>
        /// 设置或获取当前文本的默认段落属性。设置之后，只影响新变更的文本，不影响之前的文本
        /// </summary>
        public ParagraphProperty CurrentParagraphProperty { set; get; }

        /// <summary>
        /// 获取当前文本的默认字符属性
        /// </summary>
        public IReadOnlyRunProperty CurrentRunProperty { private set; get; }

        #endregion

        #region 公开属性

        /// <summary>
        /// 文档的字符数量。段落之间，使用 `\r\n` 换行符，加入计算为两个字符
        /// </summary>
        public int CharCount
        {
            get
            {
                var sum = 0;
                foreach (var paragraphData in TextRunManager.ParagraphManager.GetParagraphList())
                {
                    sum += paragraphData.CharCount;
                    // 加上换行符的字符
                    sum += ParagraphData.DelimiterLength;
                }

                if (sum > 0)
                {
                    // 证明存在一段以上，那减去最后一段多加上的换行符
                    sum -= ParagraphData.DelimiterLength;
                }

                return sum;
            }
        }

        #region 选择和光标

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
                var args = new TextEditorValueChangeEventArgs<CaretOffset>(oldValue,value);
                InternalCurrentCaretOffsetChanging?.Invoke(this,args);

                // todo 处理越界
                _currentCaretOffset = value;

                // 如果当前的进入不是由选择范围触发的，那么更新选择范围
                if (_isCurrentSelectionChanging is false)
                {
                    CurrentSelection = new Selection(value, 0);
                }

                _isCurrentCaretOffsetChanging = false;
                InternalCurrentCaretOffsetChanged?.Invoke(this,args);
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
            private set
            {
                var oldValue = _currentSelection;

                var args = new TextEditorValueChangeEventArgs<Selection>(oldValue,value);
                _isCurrentSelectionChanging = true;
                InternalCurrentSelectionChanging?.Invoke(this,args);

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

        #endregion

        #endregion

        #region 公开方法

        /// <summary>
        /// 设置当前文本的默认字符属性
        /// </summary>
        public void SetDefaultTextRunProperty(Action<RunProperty> config)
        {
            CurrentRunProperty = CurrentRunProperty.BuildNewProperty(config);
        }

        /// <summary>
        /// 追加一段文本
        /// </summary>
        /// 其实这个方法不应该放在这里
        internal void AppendText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                // 空白内容
                return;
            }

            TextEditor.AddLayoutReason(nameof(AppendText));

            //// 追加在字符数量，也就是最末
            //EditAndReplaceRun(this.GetDocumentEndSelection(), new TextRun(text));

            InternalDocumentChanging?.Invoke(this, EventArgs.Empty);

            var textRun = new TextRun(text);
            TextRunManager.Append(textRun);

            // todo 更新光标

            InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void EditAndReplaceRun(Selection selection, IImmutableRun run)
        {
            InternalDocumentChanging?.Invoke(this, EventArgs.Empty);
            // 这里只处理数据变更，后续渲染需要通过 InternalDocumentChanged 事件触发

            TextRunManager.Replace(selection, run);

            // todo 更新光标

            InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}

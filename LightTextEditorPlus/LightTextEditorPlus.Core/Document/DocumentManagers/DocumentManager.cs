
using LightTextEditorPlus.Core.Document.Segments;

using System;
using LightTextEditorPlus.Core.Carets;
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
        /// 文档的字符数量
        /// </summary>
        /// todo 文档的字符数量
        public int CharCount { get; }

        /// <summary>
        /// 获取或设置当前光标位置
        /// </summary>
        public CaretOffset CurrentCaretOffset
        {
            set
            {
                // todo 设置光标的选择范围
                throw new NotImplementedException($"还没实现选择功能");
            }
            get
            {
                throw new NotImplementedException($"还没实现选择功能");
            }
        }

        /// <summary>
        /// 获取或设置当前的选择范围
        /// </summary>
        /// 当没有选择时，将和 <see cref="CurrentCaretOffset"/> 相同
        public Selection CurrentSelection
        {
            set
            {
                throw new NotImplementedException($"还没实现选择功能");
            }
            get
            {
                throw new NotImplementedException($"还没实现选择功能");
            }
        }

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

            // 追加在字符数量，也就是最末
            EditAndReplaceRun(this.GetDocumentEndSelection(), new TextRun(text));
        }

        public void EditAndReplaceRun(Selection selection, IImmutableRun run)
        {
            InternalDocumentChanging?.Invoke(this, EventArgs.Empty);
            // 这里只处理数据变更，后续渲染需要通过 InternalDocumentChanged 事件触发

            // 先放在末尾
            TextRunManager.Replace(selection, run);

            InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}

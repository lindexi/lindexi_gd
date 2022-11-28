using System;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Platform;

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
        public TextEditor TextEditor { get; }

        /// <summary>
        /// 文档的宽度
        /// </summary>
        internal double DocumentWidth { set; get; } = double.PositiveInfinity;

        /// <summary>
        /// 文档的高度
        /// </summary>
        internal double DocumentHeight { set; get; } = double.PositiveInfinity;

        internal TextRunManager TextRunManager { get; }

        private CaretManager CaretManager => TextEditor.CaretManager;

        #region 事件

        /// <summary>
        /// 给内部提供的文档开始变更事件
        /// </summary>
        internal event EventHandler? InternalDocumentChanging;

        /// <summary>
        /// 给内部提供的文档变更完成事件
        /// </summary>
        internal event EventHandler? InternalDocumentChanged;

        #endregion

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
        /// 文档的字符数量。段落之间，使用 `\r\n` 换行符，加入计算为两个字符。包含项目符号
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

        #endregion

        #region 公开方法

        /// <summary>
        /// 设置当前文本的默认字符属性
        /// </summary>
        public void SetDefaultTextRunProperty(Action<IReadOnlyRunProperty> config)
        {
            CurrentRunProperty = TextEditor.PlatformRunPropertyCreator.BuildNewProperty(config, CurrentRunProperty);
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

            CaretManager.CurrentCaretOffset = new CaretOffset(CharCount);

            InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void EditAndReplaceRun(Selection selection, IImmutableRun run)
        {
            InternalDocumentChanging?.Invoke(this, EventArgs.Empty);
            // 这里只处理数据变更，后续渲染需要通过 InternalDocumentChanged 事件触发

            // todo 此时文本应该继承的样式是哪个？光标前的字符？还是被选择的文本的样式？被选择的文本有多个样式？
            TextRunManager.Replace(selection, run);

            // todo 更新光标

            InternalDocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}

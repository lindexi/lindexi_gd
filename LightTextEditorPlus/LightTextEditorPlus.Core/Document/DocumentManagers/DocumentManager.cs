
using System;
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
            TextRunManager = new TextRunManager(this);

            CurrentParagraphProperty = new ParagraphProperty();
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

        public IReadonlyParagraphProperty CurrentParagraphProperty { private set; get; }

        #endregion


        #region 公开方法


        /// <summary>
        /// 设置当前文本的默认段落属性。设置之后，只影响新变更的文本，不影响之前的文本
        /// </summary>
        public void SetDefaultParagraphProperty()
        {

        }

        /// <summary>
        /// 设置当前文本的默认字符属性
        /// </summary>
        public void SetDefaultTextRunProperty()
        {

        }

        #endregion
    }
}

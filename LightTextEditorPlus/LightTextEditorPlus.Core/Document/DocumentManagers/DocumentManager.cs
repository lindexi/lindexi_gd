
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Document.DocumentManagers
{
    /// <summary>
    /// 提供文档管理，只提供数据管理
    /// </summary>
    public class DocumentManager
    {
        public DocumentManager(TextEditor textEditor)
        {
            TextEditor = textEditor;
            TextRunManager = new TextRunManager(this);
        }

        public TextEditorCore TextEditor { get; }

        /// <summary>
        /// 文档的宽度
        /// </summary>
        internal double DocumentWidth { set; get; }

        /// <summary>
        /// 文档的高度
        /// </summary>
        internal double DocumentHeight { set; get; }

        internal TextRunManager TextRunManager { get; } 
    }


}

using System;

namespace LightTextEditorPlus.Core.Document.DocumentEventArgs
{
    /// <summary>
    /// 文档变更事件参数
    /// </summary>
    internal class DocumentChangeEventArgs : EventArgs
    {
        /// <summary>
        /// 文档变更事件参数
        /// </summary>
        /// <param name="documentChangeKind"></param>
        public DocumentChangeEventArgs(DocumentChangeKind documentChangeKind)
        {
            DocumentChangeKind = documentChangeKind;
        }

        /// <summary></summary>
        public DocumentChangeKind DocumentChangeKind { get; init; }

    }
}

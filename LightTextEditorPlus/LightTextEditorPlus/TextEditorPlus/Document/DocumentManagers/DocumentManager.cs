using System;
using System.Collections.Generic;
using System.Text;

namespace LightTextEditorPlus.TextEditorPlus.Document.DocumentManagers
{
    public class DocumentManager
    {
        public DocumentManager(TextEditor textEditor)
        {
            
        }

        /// <summary>
        /// 文档的宽度
        /// </summary>
        internal double DocumentWidth { set; get; }

        /// <summary>
        /// 文档的高度
        /// </summary>
        internal double DocumentHeight { set; get; }
    }
}

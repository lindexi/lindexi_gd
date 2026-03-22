#if USE_AllInOne || !USE_MauiGraphics && !USE_SKIA

using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Events;

using System;

namespace LightTextEditorPlus
{
    // 这里存放多个平台的共享代码
    [APIConstraint("TextEditor.Inheritable.Shared.txt")]
    partial class TextEditor
    {
        #region 可基类重写方法

        /// <summary>
        /// 当触发了准备上下文菜单事件时调用。可以在这里添加自定义的菜单项
        /// </summary>
        protected internal virtual void OnRaisePrepareContextMenuEvent(PrepareContextMenuEventArgs args)
        {
            RaisePrepareContextMenuEvent(args);
        }

        /// <summary>
        /// 构建自定义的撤销恢复提供器
        /// </summary>
        /// <returns></returns>
        protected internal virtual ITextEditorUndoRedoProvider? BuildCustomTextEditorUndoRedoProvider()
        {
            return null;
        }

        /// <summary>
        /// 构建自定义的文本日志
        /// </summary>
        /// <returns></returns>
        protected internal virtual ITextLogger? BuildCustomTextLogger()
        {
            return null;
        }

        #endregion
    }
}
#endif
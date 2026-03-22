#if USE_AllInOne || !USE_MauiGraphics && !USE_SKIA

using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Events;

using System;

namespace LightTextEditorPlus
{
    // 这里存放多个平台的共享代码
    [APIConstraint("TextEditor.Event.Shared.txt")]
    partial class TextEditor
    {
        /// <summary>
        /// 这是公共的代码，不同平台都会用到。在布局完成之后，会触发这个事件
        /// </summary>
        /// <param name="layoutCompletedEventArgs"></param>
        private void OnLayoutCompleted(LayoutCompletedEventArgs layoutCompletedEventArgs)
        {
            InternalLayoutCompleted?.Invoke(this, layoutCompletedEventArgs);

            LayoutCompleted?.Invoke(this, layoutCompletedEventArgs);
        }

        /// <summary>
        /// 框架内部的布局完成事件。用于确保框架内的布局事件会比上层业务更快被触发
        /// </summary>
        internal event EventHandler<LayoutCompletedEventArgs>? InternalLayoutCompleted;

        /// <summary>
        /// 文档布局完成事件
        /// </summary>
        public event EventHandler<LayoutCompletedEventArgs>? LayoutCompleted;

        /// <summary>
        /// 准备上下文菜单事件。当用户右击文本编辑器时会触发这个事件，可以在这里添加自定义的菜单项
        /// </summary>
        public event EventHandler<PrepareContextMenuEventArgs>? PrepareContextMenu; 
        private void RaisePrepareContextMenuEvent(PrepareContextMenuEventArgs args)
        {
            PrepareContextMenu?.Invoke(this, args);
        }
    }
}
#endif
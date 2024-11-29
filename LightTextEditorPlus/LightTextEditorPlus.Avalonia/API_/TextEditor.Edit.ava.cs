using Avalonia.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Events;

namespace LightTextEditorPlus
{
    // 此文件存放编辑相关的方法
    [APIConstraint("TextEditor.Edit.Input.txt")]
    [APIConstraint("TextEditor.Edit.txt")]
    partial class TextEditor
    {
        #region 编辑模式

        /// <summary>
        /// 是否进入用户编辑模式。进入用户编辑模式将闪烁光标，支持输入法输入
        /// </summary>
        public bool IsInEditingInputMode
        {
            set
            {
                if (_isInEditingInputMode == value)
                {
                    return;
                }

                //EnsureEditInit();

                Logger.LogDebug(value ? "进入用户编辑模式" : "退出用户编辑模式");

                _isInEditingInputMode = value;

                if (value)
                {
                    Focus();
                }

                IsInEditingInputModeChanged?.Invoke(this, EventArgs.Empty);

                InvalidateVisual();
            }
            get => _isInEditingInputMode;
        }

        private bool _isInEditingInputMode = false;

        /// <summary>
        /// 是否进入编辑的模式变更完成事件
        /// </summary>
        public event EventHandler? IsInEditingInputModeChanged;

        #endregion

        #region Style

        #region RunProperty

        #endregion

        #endregion

        #region 输入

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.AppendText"/>
        public void AppendText(string text) => SkiaTextEditor.AppendText(text);

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.AppendRun"/>
        public void AppendRun(SkiaTextRun textRun) => SkiaTextEditor.AppendRun(textRun);

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.EditAndReplace"/>
        public void EditAndReplace(string text, Selection? selection = null) => SkiaTextEditor.EditAndReplace(text, selection);

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.EditAndReplaceRun"/>
        public void EditAndReplaceRun(SkiaTextRun textRun, Selection? selection = null) => SkiaTextEditor.EditAndReplaceRun(textRun, selection);

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Backspace"/>
        public void Backspace() => SkiaTextEditor.Backspace();

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Delete"/>
        public void Delete() => SkiaTextEditor.Delete();

        /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Remove"/>
        public void Remove(in Selection selection) => SkiaTextEditor.Remove(in selection);

        #endregion

        #region 事件
        /// <summary>
        /// 当设置样式时触发
        /// </summary>
        public event EventHandler<StyleChangeEventArgs>? StyleChanging;

        /// <summary>
        /// 当设置样式后触发
        /// </summary>
        public event EventHandler<StyleChangeEventArgs>? StyleChanged;

        internal void OnStyleChanging(StyleChangeEventArgs styleChangeEventArgs)
        {
            StyleChanging?.Invoke(this, styleChangeEventArgs);
        }

        internal void OnStyleChanged(StyleChangeEventArgs styleChangeEventArgs)
        {
            StyleChanged?.Invoke(this, styleChangeEventArgs);
        }
        #endregion
    }
}

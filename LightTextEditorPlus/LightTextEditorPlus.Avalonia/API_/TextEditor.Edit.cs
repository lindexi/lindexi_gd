using Avalonia.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Events;

namespace LightTextEditorPlus
{
    // 此文件存放编辑相关的方法
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

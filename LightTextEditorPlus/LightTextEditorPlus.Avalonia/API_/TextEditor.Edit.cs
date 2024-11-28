using Avalonia.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                //Logger.LogDebug(value ? "进入用户编辑模式" : "退出用户编辑模式");

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

        #endregion
    }
}

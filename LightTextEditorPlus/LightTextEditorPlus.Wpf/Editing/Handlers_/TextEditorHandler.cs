using System;
using System.Windows.Input;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Editing;

public partial class TextEditorHandler
{
    /// <summary>
    /// 键盘按下事件
    /// </summary>
    /// <param name="e"></param>
    public virtual void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Insert)
        {
            SwitchOvertypeMode();
            e.Handled = true;
            return;
        }
        else if (e.Key == Key.Enter)
        {
            BreakLine();
            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// 框架内触发的文本输入事件
    /// </summary>
    /// <param name="e"></param>
    public virtual void OnTextInput(TextCompositionEventArgs e)
    {
        if (e.Handled ||
            string.IsNullOrEmpty(e.Text) ||
            e.Text == "\x1b" ||
            // 退格键 \b 键
            e.Text == "\b" ||
            //emoji包围符
            e.Text == "\ufe0f")
            return;

        RawTextInput(e.Text);
    }

    #region 方向键

    public virtual partial void MoveCaret(CaretMoveType type)
    {
        type = TransformCaretMove(type);
        TextEditor.TextEditorCore.MoveCaret(type);
    }

    /// <summary>
    /// 根据文本框实际的视觉上的旋转角度，优化键盘方向。让键盘方向控制的光标符合正视觉方向
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private CaretMoveType TransformCaretMove(CaretMoveType type)
    {
        CaretMoveType returnValue = (CaretMoveType) CaretTransformDirectionHelper.TransformDirection((int) type, TextEditor);

        return returnValue;
    }

    #endregion
}

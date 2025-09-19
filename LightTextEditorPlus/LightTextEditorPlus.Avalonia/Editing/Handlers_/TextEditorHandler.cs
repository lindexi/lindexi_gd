using Avalonia.Input;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Utils.Patterns;

namespace LightTextEditorPlus.Editing;

public partial class TextEditorHandler
{
    private MouseHandler MouseHandler => _mouseHandler ??= new MouseHandler(TextEditor);
    private MouseHandler? _mouseHandler;

    #region 鼠标

    public virtual void OnPointerPressed(PointerPressedEventArgs e)
    {
        MouseHandler.OnPointerPressed(TextEditor, e);
    }

    public virtual void OnPointerMoved(PointerEventArgs e)
    {
        MouseHandler.OnPointerMoved(TextEditor, e);
    }

    public virtual void OnPointerReleased(PointerReleasedEventArgs e)
    {
        MouseHandler.OnPointerReleased(TextEditor, e);
    }

    #endregion

    #region 键盘

    public virtual void OnTextInput(TextInputEventArgs e)
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

    public virtual void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            TextEditorCore.Delete();
            return;
        }
        else if (e.Key == Key.Back)
        {
            TextEditorCore.Backspace();
            return;
        }
        else if (e.Key == Key.Enter)
        {
            TextEditorCore.EditAndReplace("\n");
            return;
        }
        else if (e.Key == Key.Insert)
        {
           TextEditor. IsOvertypeMode = !TextEditor.IsOvertypeMode;
            return;
        }

        if (TextEditorCore.IsDirty)
        {
            // 如果有明确布局的话，可以在这里加上明确布局
            TextEditor.ForceLayout();
        }

        if (e.KeyModifiers == KeyModifiers.None)
        {
            if (e.Key == Key.Up)
            {
                TextEditorCore.MoveCaret(CaretMoveType.UpByLine);
            }
            else if (e.Key == Key.Down)
            {
                TextEditorCore.MoveCaret(CaretMoveType.DownByLine);
            }
            else if (e.Key == Key.Left)
            {
                TextEditorCore.MoveCaret(CaretMoveType.LeftByCharacter);
            }
            else if (e.Key == Key.Right)
            {
                TextEditorCore.MoveCaret(CaretMoveType.RightByCharacter);
            }
        }
    }

    public virtual void OnKeyUp(KeyEventArgs e)
    {
        if (!TextEditor.IsInEditingInputMode)
        {
            // 没有进入编辑模式，不处理键盘事件
            return;
        }
    }

    #endregion

    #region 剪贴板


    #endregion
}
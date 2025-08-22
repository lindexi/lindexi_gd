using Avalonia.Input;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Utils.Patterns;

namespace LightTextEditorPlus.Editing;

public class TextEditorHandler
{
    public TextEditorHandler(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    private TextEditor TextEditor { get; }
    private TextEditorCore TextEditorCore => TextEditor.TextEditorCore;

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

        //如果是由两个Unicode码组成的Emoji的其中一个Unicode码，则等待第二个Unicode码的输入后合并成一个字符串作为一个字符插入
        if (RegexPatterns.Utf16SurrogatesPattern.ContainInRange(e.Text))
        {
            if (string.IsNullOrEmpty(_emojiCache))
            {
                _emojiCache += e.Text;
            }
            else
            {
                _emojiCache += e.Text;

                PerformInput(_emojiCache);
                _emojiCache = string.Empty;
            }
        }
        else
        {
            _emojiCache = string.Empty;
            PerformInput(e.Text);
        }
    }

    protected virtual void PerformInput(string text)
    {
        Selection? selection = null;
        if (TextEditor.IsOvertypeMode)
        {
            selection = TextEditorCore.GetCurrentOvertypeModeSelection(text.Length);
        }

        TextEditorCore.EditAndReplace(text, selection);
    }

    /// <summary>
    /// 如果是由两个Unicode码组成的Emoji的其中一个Unicode码，则等待第二个Unicode码的输入后合并成一个字符串作为一个字符插入
    /// 用于接收第一个字符
    /// </summary>
    private string _emojiCache = string.Empty;

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
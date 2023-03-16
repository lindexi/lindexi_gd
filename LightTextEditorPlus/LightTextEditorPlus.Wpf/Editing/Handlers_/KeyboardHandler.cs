using System.Windows.Documents;
using System.Windows.Input;

namespace LightTextEditorPlus.Editing;

/// <summary>
/// 用来处理键盘的交互行为
/// </summary>
/// 不包含 IME 输入法的行为。关于 IME 输入法相关，放在 IME 相关模块里处理
internal class KeyboardHandler
{
    public KeyboardHandler(TextEditor textEditor)
    {
        _textArea = textEditor;

        ////光标移动
        //_textArea.CommandBindings.Add(new CommandBinding(EditingCommands.MoveLeftByCharacter,
        //    MoveCaret(CaretMoveType.LeftByCharacter)));

        //编辑
        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.Backspace, OnBackspace));
        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.Delete, OnDelete));

        // 设置快捷键
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.Backspace, Key.Back, ModifierKeys.None));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.Delete, Key.Delete, ModifierKeys.None));
    }

    private void OnDelete(object sender, ExecutedRoutedEventArgs e)
    {
        TextEditor.TextEditorCore.Delete();
    }

    private void OnBackspace(object sender, ExecutedRoutedEventArgs e)
    {
        TextEditor.TextEditorCore.Backspace();
    }

    private TextEditor TextEditor => _textArea;
    private readonly TextEditor _textArea;
}
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Editing;

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

        //光标移动
        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.MoveLeftByCharacter,
            MoveCaret(CaretMoveType.LeftByCharacter)));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.MoveLeftByCharacter, Key.Left, ModifierKeys.None));

        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.MoveRightByCharacter,
            MoveCaret(CaretMoveType.RightByCharacter)));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.MoveRightByCharacter, Key.Right, ModifierKeys.None));

        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.MoveUpByLine,
            MoveCaret(CaretMoveType.UpByLine)));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.MoveUpByLine, Key.Up, ModifierKeys.None));

        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.MoveDownByLine,
            MoveCaret(CaretMoveType.DownByLine)));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.MoveDownByLine, Key.Down, ModifierKeys.None));

        //编辑
        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.Backspace, OnBackspace));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.Backspace, Key.Back, ModifierKeys.None));

        _textArea.CommandBindings.Add(new CommandBinding(EditingCommands.Delete, OnDelete));
        _textArea.InputBindings.Add(new KeyBinding(EditingCommands.Delete, Key.Delete, ModifierKeys.None));

        textEditor.KeyDown += TextEditor_KeyDown;
    }

    private TextEditorHandler TextEditorHandler => TextEditor.TextEditorHandler;

    private void TextEditor_KeyDown(object sender, KeyEventArgs e)
    {
        TextEditorHandler.OnKeyDown(e);
    }

    #region 删除

    private static void OnDelete(object sender, ExecutedRoutedEventArgs e)
    {
        var textEditor = (TextEditor) e.Source;
        textEditor.TextEditorHandler.Delete();
    }

    private static void OnBackspace(object sender, ExecutedRoutedEventArgs e)
    {
        var textEditor = (TextEditor) e.Source;
        textEditor.TextEditorHandler.Backspace();
    }

    #endregion

    #region 方向键

    private ExecutedRoutedEventHandler MoveCaret(CaretMoveType moveType)
    {
        return (o, args) =>
        {
            //var textEditor = (TextEditor)args.Source; // 就是从 TextEditor 订阅的
            TextEditorHandler.MoveCaret(moveType);
        };
    }
   
    #endregion

    private TextEditor TextEditor => _textArea;

    /// <summary>
    /// 文本交互范围
    /// </summary>
    /// 只是为了方便抄代码，后续可以干掉
    private readonly TextEditor _textArea;
}
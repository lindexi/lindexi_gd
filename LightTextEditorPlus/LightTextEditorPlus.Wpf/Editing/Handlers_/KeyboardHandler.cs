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
/// 现在拆开 KeyboardHandler 和 TextEditorHandler 的重要原因在于 KeyboardHandler 需要监听大量的命令绑定，这个过程是耗时的。想想，如果放在表格里面，表格里面有大量文本框，如果一开始全都监听，那此时的开销就太大了。独立在这个类型里面，可以在当文本接受编辑时，才监听，尽可能延迟执行
internal class KeyboardHandler
{
    public KeyboardHandler(TextEditor textEditor)
    {
        TextEditor = textEditor;

        //光标移动
        TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.MoveLeftByCharacter,
            MoveCaret(CaretMoveType.LeftByCharacter)));
        TextEditor.InputBindings.Add(new KeyBinding(EditingCommands.MoveLeftByCharacter, Key.Left, ModifierKeys.None));

        TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.MoveRightByCharacter,
            MoveCaret(CaretMoveType.RightByCharacter)));
        TextEditor.InputBindings.Add(new KeyBinding(EditingCommands.MoveRightByCharacter, Key.Right, ModifierKeys.None));

        TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.MoveUpByLine,
            MoveCaret(CaretMoveType.UpByLine)));
        TextEditor.InputBindings.Add(new KeyBinding(EditingCommands.MoveUpByLine, Key.Up, ModifierKeys.None));

        TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.MoveDownByLine,
            MoveCaret(CaretMoveType.DownByLine)));
        TextEditor.InputBindings.Add(new KeyBinding(EditingCommands.MoveDownByLine, Key.Down, ModifierKeys.None));

        TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.MoveToLineStart,
            MoveCaret(CaretMoveType.LineStart)));
        TextEditor.InputBindings.Add(new KeyBinding(EditingCommands.MoveToLineStart, Key.Home, ModifierKeys.None));

        TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.MoveToLineEnd,
            MoveCaret(CaretMoveType.LineEnd)));
        TextEditor.InputBindings.Add(new KeyBinding(EditingCommands.MoveToLineEnd, Key.End, ModifierKeys.None));

        //编辑
        TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.Backspace, OnBackspace));
        TextEditor.InputBindings.Add(new KeyBinding(EditingCommands.Backspace, Key.Back, ModifierKeys.None));

        TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.Delete, OnDelete));
        TextEditor.InputBindings.Add(new KeyBinding(EditingCommands.Delete, Key.Delete, ModifierKeys.None));

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

    private TextEditor TextEditor { get; }
}
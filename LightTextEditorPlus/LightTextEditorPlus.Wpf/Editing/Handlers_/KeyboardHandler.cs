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

        // 光标移动
        Add(Key.Left, EditingCommands.MoveLeftByCharacter, MoveCaret(CaretMoveType.LeftByCharacter));
        Add(Key.Right, EditingCommands.MoveRightByCharacter, MoveCaret(CaretMoveType.RightByCharacter));
        Add(Key.Up, EditingCommands.MoveUpByLine, MoveCaret(CaretMoveType.UpByLine));
        Add(Key.Down, EditingCommands.MoveDownByLine, MoveCaret(CaretMoveType.DownByLine));

        Add(Key.Home, EditingCommands.MoveToLineStart, MoveCaret(CaretMoveType.LineStart));
        Add(Key.End, EditingCommands.MoveToLineEnd, MoveCaret(CaretMoveType.LineEnd));

        Add(Key.Home, ModifierKeys.Control, EditingCommands.MoveToDocumentStart, MoveCaret(CaretMoveType.DocumentStart));
        Add(Key.End, ModifierKeys.Control, EditingCommands.MoveToDocumentEnd, MoveCaret(CaretMoveType.DocumentEnd));

        // 编辑
        Add(Key.Back, EditingCommands.Backspace, OnBackspace);
        Add(Key.Delete, EditingCommands.Delete, OnDelete);

        // 输入状态
        Add(Key.Insert, EditingCommands.ToggleInsert, OnToggleInsert);

        // 默认命令。默认命令都不用绑定快捷键，因为系统（框架）已经绑定好了
        // 剪贴板
        Add(ApplicationCommands.Copy, OnCopy);
        Add(ApplicationCommands.Cut, OnCut);
        Add(ApplicationCommands.Paste, OnPaste);

        // 样式变更-无默认快捷键绑定
        Add(EditingCommands.ToggleItalic, (s, e) =>
        {
            TextEditor.ToggleItalic();
        });
        Add(EditingCommands.ToggleBold, (s, e) =>
        {
            TextEditor.ToggleBold();
        });
        Add(EditingCommands.ToggleUnderline, (s, e) =>
        {
            TextEditor.ToggleUnderline();
        });
        Add(EditingCommands.ToggleSuperscript, (s, e) =>
        {
            TextEditor.ToggleSuperscript();
        });
        Add(EditingCommands.ToggleSubscript, (s, e) =>
        {
            TextEditor.ToggleSubscript();
        });

        textEditor.KeyDown += TextEditor_KeyDown;
    }

    private void Add(Key key, ICommand command, ExecutedRoutedEventHandler handler)
    {
        TextEditor.CommandBindings.Add(new CommandBinding(command,
            handler));
        TextEditor.InputBindings.Add(new KeyBinding(command, key, ModifierKeys.None));
    }

    private void Add(Key key, ModifierKeys modifierKeys, ICommand command, ExecutedRoutedEventHandler handler)
    {
        TextEditor.CommandBindings.Add(new CommandBinding(command,
            handler));
        TextEditor.InputBindings.Add(new KeyBinding(command, key, modifierKeys));
    }

    private void Add(ICommand command, ExecutedRoutedEventHandler handler)
    {
        TextEditor.CommandBindings.Add(new CommandBinding(command,
            handler));
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

    private void OnToggleInsert(object sender, ExecutedRoutedEventArgs e)
    {
        TextEditorHandler.ToggleInsert();
    }

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

    #region 剪贴板

    private void OnCopy(object sender, ExecutedRoutedEventArgs e)
    {
        TextEditorHandler.OnCopy(sender, e);
    }

    private void OnCut(object sender, ExecutedRoutedEventArgs e)
    {
        TextEditorHandler.OnCut(sender, e);
    }

    private void OnPaste(object sender, ExecutedRoutedEventArgs e)
    {
        TextEditorHandler.OnPaste(sender, e);
    }

    #endregion
}
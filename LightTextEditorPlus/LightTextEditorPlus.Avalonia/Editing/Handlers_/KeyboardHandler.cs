using System;
using Avalonia.Input;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Editing;

namespace LightTextEditorPlus.Editing;

/// <summary>
/// 用来处理键盘的交互行为
/// </summary>
/// 不包含 IME 输入法的行为。关于 IME 输入法相关，放在 IME 相关模块里处理
/// 现在拆开 KeyboardHandler 和 TextEditorHandler 的重要原因在于 KeyboardHandler 需要监听大量的命令绑定，这个过程是耗时的。想想，如果放在表格里面，表格里面有大量文本框，如果一开始全都监听，那此时的开销就太大了。独立在这个类型里面，可以在当文本接受编辑时，才监听，尽可能延迟执行
///
/// 在 Avalonia 里面没有内建的命令，所以这里的命令绑定只能自己定义
/// https://github.com/AvaloniaUI/Avalonia/discussions/15963
internal class KeyboardHandler
{
    public KeyboardHandler(TextEditor textEditor)
    {
        TextEditor = textEditor;

        AddShortCut(new KeyGesture(Key.C, KeyModifiers.Control), TextEditorHandler.OnCopy);
        AddShortCut(new KeyGesture(Key.X, KeyModifiers.Control), TextEditorHandler.OnCut);
        AddShortCut(new KeyGesture(Key.V, KeyModifiers.Control), TextEditorHandler.OnPaste);


        void AddShortCut(KeyGesture gesture, Action command)
        {
            TextEditor.KeyBindings.Add(new KeyBinding()
            {
                Gesture = gesture,
                Command = new TextEditorCommand(command)
            });
        }

        //// 默认命令。默认命令都不用绑定快捷键，因为系统（框架）已经绑定好了
        //// 剪贴板
        //TextEditor.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopy));
        //TextEditor.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, OnCut));
        //TextEditor.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, OnPaste));

        //// 样式变更-无默认快捷键绑定
        //TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.ToggleItalic, (s, e) =>
        //{
        //    TextEditor.ToggleItalic();
        //}));
        //TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.ToggleBold, (s, e) =>
        //{
        //    TextEditor.ToggleBold();
        //}));
        //TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.ToggleUnderline, (s, e) =>
        //{
        //    TextEditor.ToggleUnderline();
        //}));
        //TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.ToggleSuperscript, (s, e) =>
        //{
        //    TextEditor.ToggleSuperscript();
        //}));
        //TextEditor.CommandBindings.Add(new CommandBinding(EditingCommands.ToggleSubscript, (s, e) =>
        //{
        //    TextEditor.ToggleSubscript();
        //}));

        //textEditor.KeyDown += TextEditor_KeyDown;
    }

    private TextEditorHandler TextEditorHandler => TextEditor.TextEditorHandler;

    //private void TextEditor_KeyDown(object sender, KeyEventArgs e)
    //{
    //    TextEditorHandler.OnKeyDown(e);
    //}

    //#region 删除

    //private static void OnDelete(object sender, ExecutedRoutedEventArgs e)
    //{
    //    var textEditor = (TextEditor) e.Source;
    //    textEditor.TextEditorHandler.Delete();
    //}

    //private static void OnBackspace(object sender, ExecutedRoutedEventArgs e)
    //{
    //    var textEditor = (TextEditor) e.Source;
    //    textEditor.TextEditorHandler.Backspace();
    //}

    //#endregion

    //private void OnToggleInsert(object sender, ExecutedRoutedEventArgs e)
    //{
    //    TextEditorHandler.ToggleInsert();
    //}

    //#region 方向键

    //private ExecutedRoutedEventHandler MoveCaret(CaretMoveType moveType)
    //{
    //    return (o, args) =>
    //    {
    //        //var textEditor = (TextEditor)args.Source; // 就是从 TextEditor 订阅的
    //        TextEditorHandler.MoveCaret(moveType);
    //    };
    //}
   
    //#endregion

    private TextEditor TextEditor { get; }

    #region 剪贴板

    //private void OnCopy(object sender, ExecutedRoutedEventArgs e)
    //{
    //    TextEditorHandler.OnCopy(sender, e);
    //}

    //private void OnCut(object sender, ExecutedRoutedEventArgs e)
    //{
    //    TextEditorHandler.OnCut(sender, e);
    //}

    //private void OnPaste(object sender, ExecutedRoutedEventArgs e)
    //{
    //    TextEditorHandler.OnPaste(sender, e);
    //}

    #endregion
}
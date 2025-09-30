using System;

using Avalonia.Input;

using LightTextEditorPlus.Core.Carets;

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

        // 光标移动
        AddShortCut(new KeyGesture(Key.Home, KeyModifiers.None), MoveToLineStart);
        AddShortCut(new KeyGesture(Key.End, KeyModifiers.None), MoveToLineEnd);

        AddShortCut(new KeyGesture(Key.Home, KeyModifiers.Control), MoveToDocumentStart);
        AddShortCut(new KeyGesture(Key.End, KeyModifiers.Control), MoveToDocumentEnd);

        // 剪贴板
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
    }

    private void MoveToDocumentStart()
    {
        TextEditorHandler.InputEnsureLayout();
        TextEditorHandler.MoveCaret(CaretMoveType.DocumentStart);
    }

    private void MoveToDocumentEnd()
    {
        TextEditorHandler.InputEnsureLayout();
        TextEditorHandler.MoveCaret(CaretMoveType.DocumentEnd);
    }

    private void MoveToLineStart()
    {
        TextEditorHandler.InputEnsureLayout();
        TextEditorHandler.MoveCaret(CaretMoveType.LineStart);
    }

    private void MoveToLineEnd()
    {
        TextEditorHandler.InputEnsureLayout();
        TextEditorHandler.MoveCaret(CaretMoveType.LineEnd);
    }

    private TextEditorHandler TextEditorHandler => TextEditor.TextEditorHandler;

    private TextEditor TextEditor { get; }
}
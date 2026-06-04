using Avalonia.Input;
using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;
using SimpleWrite.ViewModels;

namespace SimpleWrite.Business.ShortcutManagers;

static class ShortcutManagerHelper
{
    /// <summary>
    /// 添加默认的快捷键
    /// </summary>
    public static void AddDefaultShortcut(EditorViewModel viewModel)
    {
        var shortcutManager = viewModel.ShortcutManager;
        shortcutManager.AddShortcut(KeyModifiers.Control, Key.S, "SaveDocument", () =>
        {
            _ = viewModel.SaveDocument();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control | KeyModifiers.Shift, Key.S, "SaveDocumentAs", () =>
        {
            _ = viewModel.SaveDocumentAs();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.O, "OpenDocument", () =>
        {
            _ = viewModel.OpenDocumentAsync();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.N, "NewDocument", () =>
        {
            viewModel.NewDocument();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.W, "CloseCurrentDocument", () =>
        {
            _ = viewModel.RequestCloseCurrentDocumentAsync();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.Tab, "SwitchToNextDocument", () =>
        {
            viewModel.SwitchToNextDocument();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.F, "ShowFindPanel", () =>
        {
            viewModel.MainViewModel.FindReplaceViewModel.ShowFind();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.H, "ShowReplacePanel", () =>
        {
            viewModel.MainViewModel.FindReplaceViewModel.ShowReplace();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.D, "SelectCurrentWord", context =>
        {
            var textEditor = context.CurrentTextEditor;

            var wordSelection = textEditor.GetCurrentCaretOffsetWord();
            textEditor.CurrentSelection = wordSelection;
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.Back, "DeleteForwardWord", context =>
        {
            var textEditor = context.CurrentTextEditor;

            var currentSelection = textEditor.CurrentSelection;
            if (!currentSelection.IsEmpty)
            {
                // 有选中内容时，直接删除选中
                textEditor.Remove(in currentSelection);
                return;
            }

            var caretOffset = textEditor.CurrentCaretOffset;
            if (caretOffset.Offset == 0)
            {
                // 文档开头，无法删除
                return;
            }

            var wordSelection = textEditor.GetCurrentCaretOffsetWord();

            if (wordSelection.Contains(in caretOffset)
                && wordSelection.FrontOffset.Offset != caretOffset.Offset)
            {
                // 光标在单词内部（不在单词开头），删除从单词开头到光标的内容
                var deleteSelection = new Selection(wordSelection.FrontOffset, caretOffset);
                textEditor.Remove(in deleteSelection);
            }
            else
            {
                // 光标在单词开头或单词外（如标点符号），删除前一个字符
                textEditor.Backspace();
            }
        });
    }
}
using Avalonia.Input;
using LightTextEditorPlus;

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
        shortcutManager.AddShortcut(KeyModifiers.Control, Key.S, "SaveDocument", _1 =>
        {
            _ = viewModel.SaveDocument();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control | KeyModifiers.Shift, Key.S, "SaveDocumentAs", _1 =>
        {
            _ = viewModel.SaveDocumentAs();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.O, "OpenDocument", _1 =>
        {
            _ = viewModel.OpenDocumentAsync();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.N, "NewDocument", _1 =>
        {
            viewModel.NewDocument();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.W, "CloseCurrentDocument", _1 =>
        {
            viewModel.CloseCurrentDocument();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.Tab, "SwitchToNextDocument", _1 =>
        {
            viewModel.SwitchToNextDocument();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control, Key.D, "SelectCurrentWord", context =>
        {
            var textEditor = context.CurrentTextEditor;

            var wordSelection = textEditor.GetCurrentCaretOffsetWord();
            textEditor.CurrentSelection = wordSelection;
        });
    }
}
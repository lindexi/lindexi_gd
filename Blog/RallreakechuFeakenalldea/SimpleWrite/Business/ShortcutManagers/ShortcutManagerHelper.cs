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
    }
}
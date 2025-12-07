using Avalonia.Input;

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
        shortcutManager.AddShortcut(KeyModifiers.Control, Key.S, "SaveDocument", _ =>
        {
            viewModel.SaveDocument();
        });

        shortcutManager.AddShortcut(KeyModifiers.Control | KeyModifiers.Shift, Key.S, "SaveDocumentAs", _ =>
        {
            viewModel.SaveDocumentAs();
        });
    }
}
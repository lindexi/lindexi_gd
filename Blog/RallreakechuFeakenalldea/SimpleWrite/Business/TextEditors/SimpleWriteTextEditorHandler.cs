using Avalonia.Input;

using LightTextEditorPlus;
using LightTextEditorPlus.Editing;

using SimpleWrite.Business.ShortcutManagers;

namespace SimpleWrite.Business.TextEditors;

class SimpleWriteTextEditorHandler : TextEditorHandler
{
    public SimpleWriteTextEditorHandler(TextEditor textEditor) : base(textEditor)
    {
    }

    public required ShortcutExecutor ShortcutExecutor { get; init; }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // 判断是否落在快捷键范围内
        var shortcutHandled = ShortcutExecutor.Handle(e, new ShortcutExecuteContext
        {
            CurrentTextEditor = TextEditor,
        });
        if (shortcutHandled)
        {
            // 被快捷键处理了，就不继续往下传递
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
    }
}
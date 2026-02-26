using LightTextEditorPlus;

namespace SimpleWrite.Business.ShortcutManagers;

public class ShortcutExecuteContext
{
    public required TextEditor CurrentTextEditor { get; init; }
}